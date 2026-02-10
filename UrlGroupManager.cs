using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BrowserSelector.Services;

namespace BrowserSelector
{
    /// <summary>
    /// Manages URL groups and overrides with JSON persistence
    /// </summary>
    public static class UrlGroupManager
    {
        private static readonly string ConfigDirectory = AppConfig.AppDataFolder;

        private static readonly string GroupsConfigPath = Path.Combine(ConfigDirectory, "urlgroups.json");
        private static readonly string OverridesConfigPath = Path.Combine(ConfigDirectory, "urlgroupoverrides.json");

        private static List<UrlGroup>? _cachedGroups = null;
        private static List<UrlGroupOverride>? _cachedOverrides = null;

        #region Built-in Groups

        /// <summary>
        /// Gets stable pattern IDs for built-in patterns during migration.
        /// Maps group ID + pattern text to a stable pattern ID.
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, string>> _builtInPatternIds = new()
        {
            ["builtin-m365"] = new Dictionary<string, string>
            {
                ["admin.microsoft.com"] = "m365-admin",
                ["portal.azure.com"] = "m365-azure",
                ["outlook.office.com"] = "m365-outlook",
                ["outlook.office365.com"] = "m365-outlook365",
                ["teams.microsoft.com"] = "m365-teams",
                ["forms.office.com"] = "m365-forms",
                ["sharepoint.com"] = "m365-sharepoint",
                ["onedrive.com"] = "m365-onedrive",
                ["office.com"] = "m365-office",
                ["microsoft365.com"] = "m365-m365",
                ["live.com"] = "m365-live",
                ["microsoftonline.com"] = "m365-online",
                ["azure.com"] = "m365-azurecom",
                ["dynamics.com"] = "m365-dynamics",
                ["powerbi.com"] = "m365-powerbi",
                ["powerapps.com"] = "m365-powerapps",
                ["flow.microsoft.com"] = "m365-flow"
            },
            ["builtin-google"] = new Dictionary<string, string>
            {
                ["mail.google.com"] = "google-mail",
                ["drive.google.com"] = "google-drive",
                ["docs.google.com"] = "google-docs",
                ["sheets.google.com"] = "google-sheets",
                ["slides.google.com"] = "google-slides",
                ["calendar.google.com"] = "google-calendar",
                ["meet.google.com"] = "google-meet",
                ["chat.google.com"] = "google-chat",
                ["keep.google.com"] = "google-keep",
                ["contacts.google.com"] = "google-contacts",
                ["admin.google.com"] = "google-admin",
                ["cloud.google.com"] = "google-cloud",
                ["console.cloud.google.com"] = "google-console"
            }
        };

        /// <summary>
        /// Gets the stable ID for a built-in pattern, or generates a new one.
        /// </summary>
        private static string GetStablePatternId(string groupId, string pattern)
        {
            if (_builtInPatternIds.TryGetValue(groupId, out var patterns) &&
                patterns.TryGetValue(pattern, out var id))
            {
                return id;
            }
            return Guid.NewGuid().ToString();
        }

        #endregion

        #region URL Groups CRUD

        /// <summary>
        /// Loads all URL groups from the config file, migrating legacy data if needed.
        /// </summary>
        public static List<UrlGroup> LoadGroups()
        {
            if (_cachedGroups != null)
                return _cachedGroups;

            _cachedGroups = JsonStorageService.Load<List<UrlGroup>>(GroupsConfigPath);

            // Check if migration is needed (legacy patterns exist)
            bool needsMigration = _cachedGroups.Any(g =>
                g.LegacyUrlPatterns != null && g.LegacyUrlPatterns.Count > 0 &&
                (g.UrlPatterns == null || g.UrlPatterns.Count == 0));

            if (needsMigration)
            {
                Logger.Log("Migrating URL groups from legacy string patterns to UrlPattern objects");
                JsonStorageService.CreatePreMigrationBackup(GroupsConfigPath, "string-to-urlpattern");

                foreach (var group in _cachedGroups)
                {
                    if (group.LegacyUrlPatterns != null && group.LegacyUrlPatterns.Count > 0 &&
                        (group.UrlPatterns == null || group.UrlPatterns.Count == 0))
                    {
                        group.UrlPatterns = group.LegacyUrlPatterns.Select(p => new UrlPattern
                        {
                            Id = group.IsBuiltIn ? GetStablePatternId(group.Id, p) : Guid.NewGuid().ToString(),
                            Pattern = p,
                            IsBuiltIn = group.IsBuiltIn,
                            VersionAdded = group.IsBuiltIn ? "1.0.0" : null,
                            CreatedDate = group.CreatedDate
                        }).ToList();

                        // Clear legacy patterns after migration
                        group.LegacyUrlPatterns = null;

                        Logger.Log($"Migrated {group.UrlPatterns.Count} patterns for group '{group.Name}'");
                    }
                }

                SaveGroups(_cachedGroups);
                Logger.Log("URL group migration complete");
            }

            return _cachedGroups;
        }

        /// <summary>
        /// Saves all URL groups to the config file
        /// </summary>
        public static void SaveGroups(List<UrlGroup> groups)
        {
            JsonStorageService.Save(GroupsConfigPath, groups);
            _cachedGroups = groups;
        }

        /// <summary>
        /// Adds a new URL group
        /// </summary>
        public static void AddGroup(UrlGroup group)
        {
            var groups = LoadGroups();
            group.CreatedDate = DateTime.Now;
            group.ModifiedDate = DateTime.Now;
            groups.Add(group);
            SaveGroups(groups);
        }

        /// <summary>
        /// Updates an existing URL group (preserves list order)
        /// </summary>
        public static void UpdateGroup(UrlGroup group)
        {
            var groups = LoadGroups();
            var index = groups.FindIndex(g => g.Id == group.Id);
            if (index >= 0)
            {
                group.ModifiedDate = DateTime.Now;
                groups[index] = group; // Replace at same position to preserve order
                SaveGroups(groups);
            }
        }

        /// <summary>
        /// Deletes a URL group by ID
        /// </summary>
        public static void DeleteGroup(string groupId)
        {
            var groups = LoadGroups();
            groups.RemoveAll(g => g.Id == groupId);
            SaveGroups(groups);

            // Also delete associated overrides
            var overrides = LoadOverrides();
            overrides.RemoveAll(o => o.UrlGroupId == groupId);
            SaveOverrides(overrides);
        }

        /// <summary>
        /// Gets a URL group by ID
        /// </summary>
        public static UrlGroup? GetGroup(string groupId)
        {
            return LoadGroups().FirstOrDefault(g => g.Id == groupId);
        }

        /// <summary>
        /// Imports built-in groups (adds them if they don't exist) and enables them.
        /// Called when user manually requests import.
        /// </summary>
        public static void ImportBuiltInGroups()
        {
            // Use BuiltInTemplateManager for template-based updates
            BuiltInTemplateManager.ApplyTemplateUpdates();

            // Enable all built-in groups when manually imported
            var groups = LoadGroups();
            bool changed = false;

            foreach (var group in groups.Where(g => g.IsBuiltIn && !g.IsEnabled))
            {
                group.IsEnabled = true;
                changed = true;
                Logger.Log($"Enabled built-in group '{group.Name}'");
            }

            if (changed)
            {
                SaveGroups(groups);
            }
        }

        /// <summary>
        /// Ensures built-in groups exist and applies any template updates.
        /// Called automatically when app starts.
        /// </summary>
        public static void EnsureBuiltInGroupsExist()
        {
            // Apply template updates (adds new groups disabled, merges new patterns)
            BuiltInTemplateManager.ApplyTemplateUpdates();
        }

        /// <summary>
        /// Clears the groups cache
        /// </summary>
        public static void ClearGroupsCache()
        {
            _cachedGroups = null;
        }

        #endregion

        #region URL Group Overrides CRUD

        /// <summary>
        /// Loads all URL group overrides from the config file
        /// </summary>
        public static List<UrlGroupOverride> LoadOverrides()
        {
            if (_cachedOverrides != null)
                return _cachedOverrides;

            _cachedOverrides = JsonStorageService.Load<List<UrlGroupOverride>>(OverridesConfigPath);
            return _cachedOverrides;
        }

        /// <summary>
        /// Saves all URL group overrides to the config file
        /// </summary>
        public static void SaveOverrides(List<UrlGroupOverride> overrides)
        {
            JsonStorageService.Save(OverridesConfigPath, overrides);
            _cachedOverrides = overrides;
        }

        /// <summary>
        /// Adds a new override to a URL group
        /// </summary>
        public static void AddOverride(UrlGroupOverride urlOverride)
        {
            var overrides = LoadOverrides();
            urlOverride.CreatedDate = DateTime.Now;
            overrides.Add(urlOverride);
            SaveOverrides(overrides);
        }

        /// <summary>
        /// Updates an existing override (preserves list order)
        /// </summary>
        public static void UpdateOverride(UrlGroupOverride urlOverride)
        {
            var overrides = LoadOverrides();
            var index = overrides.FindIndex(o => o.Id == urlOverride.Id);
            if (index >= 0)
            {
                overrides[index] = urlOverride; // Replace at same position to preserve order
                SaveOverrides(overrides);
            }
        }

        /// <summary>
        /// Deletes an override by ID
        /// </summary>
        public static void DeleteOverride(string overrideId)
        {
            var overrides = LoadOverrides();
            overrides.RemoveAll(o => o.Id == overrideId);
            SaveOverrides(overrides);
        }

        /// <summary>
        /// Gets all overrides for a specific URL group
        /// </summary>
        public static List<UrlGroupOverride> GetOverridesForGroup(string groupId)
        {
            return LoadOverrides().Where(o => o.UrlGroupId == groupId).ToList();
        }

        /// <summary>
        /// Clears the overrides cache
        /// </summary>
        public static void ClearOverridesCache()
        {
            _cachedOverrides = null;
        }

        #endregion

        #region URL Matching

        /// <summary>
        /// Finds a matching URL group for the given URL
        /// Returns the group and any applicable override
        /// </summary>
        public static (UrlGroup? Group, UrlGroupOverride? Override) FindMatchingGroup(string url)
        {
            var groups = LoadGroups().Where(g => g.IsEnabled).ToList();
            var overrides = LoadOverrides();

            foreach (var group in groups)
            {
                if (MatchesGroupPatterns(url, group.UrlPatterns))
                {
                    // Check for override first
                    var matchingOverride = overrides.FirstOrDefault(o =>
                        o.UrlGroupId == group.Id &&
                        url.IndexOf(o.UrlPattern, StringComparison.OrdinalIgnoreCase) >= 0);

                    return (group, matchingOverride);
                }
            }

            return (null, null);
        }

        /// <summary>
        /// Checks if a URL matches any of the patterns in the list
        /// </summary>
        private static bool MatchesGroupPatterns(string url, List<UrlPattern> patterns)
        {
            if (patterns == null || patterns.Count == 0)
                return false;

            string urlLower = url.ToLower();

            // Try to extract domain from URL
            string domain = string.Empty;
            try
            {
                var uri = new Uri(url);
                domain = uri.Host.ToLower();
            }
            catch
            {
                // If URL parsing fails, use the full URL for matching
            }

            foreach (var urlPattern in patterns)
            {
                string patternLower = urlPattern.Pattern.ToLower();

                // Check if URL contains the pattern
                if (urlLower.Contains(patternLower))
                    return true;

                // Check if domain contains or matches the pattern
                if (!string.IsNullOrEmpty(domain))
                {
                    if (domain.Contains(patternLower) || patternLower.Contains(domain))
                        return true;

                    // Check for subdomain match (e.g., "sharepoint.com" matches "contoso.sharepoint.com")
                    if (domain.EndsWith("." + patternLower) || domain == patternLower)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a disabled URL group would have matched the given URL.
        /// Used to suppress "Create Rule" notifications when a group exists but is disabled.
        /// </summary>
        public static bool HasDisabledMatchingGroup(string url)
        {
            var disabledGroups = LoadGroups().Where(g => !g.IsEnabled).ToList();

            if (disabledGroups.Count == 0)
                return false;

            foreach (var group in disabledGroups)
            {
                if (MatchesGroupPatterns(url, group.UrlPatterns))
                    return true;
            }

            return false;
        }

        #endregion
    }
}
