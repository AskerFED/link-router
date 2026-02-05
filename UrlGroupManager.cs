using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BrowserSelector
{
    /// <summary>
    /// Manages URL groups and overrides with JSON persistence
    /// </summary>
    public static class UrlGroupManager
    {
        private static readonly string ConfigDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BrowserSelector");

        private static readonly string GroupsConfigPath = Path.Combine(ConfigDirectory, "urlgroups.json");
        private static readonly string OverridesConfigPath = Path.Combine(ConfigDirectory, "urlgroupoverrides.json");

        private static List<UrlGroup>? _cachedGroups = null;
        private static List<UrlGroupOverride>? _cachedOverrides = null;

        #region Built-in Groups

        /// <summary>
        /// Returns the predefined built-in URL groups
        /// </summary>
        public static List<UrlGroup> GetBuiltInGroups()
        {
            return new List<UrlGroup>
            {
                new UrlGroup
                {
                    Id = "builtin-m365",
                    Name = "Microsoft 365",
                    Description = "Microsoft 365, Azure, and related services",
                    IsBuiltIn = true,
                    IsEnabled = true,
                    UrlPatterns = new List<string>
                    {
                        "admin.microsoft.com",
                        "portal.azure.com",
                        "outlook.office.com",
                        "outlook.office365.com",
                        "teams.microsoft.com",
                        "forms.office.com",
                        "sharepoint.com",
                        "onedrive.com",
                        "office.com",
                        "microsoft365.com",
                        "live.com",
                        "microsoftonline.com",
                        "azure.com",
                        "dynamics.com",
                        "powerbi.com",
                        "powerapps.com",
                        "flow.microsoft.com"
                    },
                    Behavior = UrlGroupBehavior.UseDefault
                },
                new UrlGroup
                {
                    Id = "builtin-google",
                    Name = "Google Suite",
                    Description = "Google Workspace and related services",
                    IsBuiltIn = true,
                    IsEnabled = true,
                    UrlPatterns = new List<string>
                    {
                        "mail.google.com",
                        "drive.google.com",
                        "docs.google.com",
                        "sheets.google.com",
                        "slides.google.com",
                        "calendar.google.com",
                        "meet.google.com",
                        "chat.google.com",
                        "keep.google.com",
                        "contacts.google.com",
                        "admin.google.com",
                        "cloud.google.com",
                        "console.cloud.google.com"
                    },
                    Behavior = UrlGroupBehavior.UseDefault
                }
            };
        }

        #endregion

        #region URL Groups CRUD

        /// <summary>
        /// Loads all URL groups from the config file
        /// </summary>
        public static List<UrlGroup> LoadGroups()
        {
            if (_cachedGroups != null)
                return _cachedGroups;

            try
            {
                if (File.Exists(GroupsConfigPath))
                {
                    var json = File.ReadAllText(GroupsConfigPath);
                    _cachedGroups = JsonSerializer.Deserialize<List<UrlGroup>>(json) ?? new List<UrlGroup>();
                    return _cachedGroups;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading URL groups: {ex.Message}");
            }

            _cachedGroups = new List<UrlGroup>();
            return _cachedGroups;
        }

        /// <summary>
        /// Saves all URL groups to the config file
        /// </summary>
        public static void SaveGroups(List<UrlGroup> groups)
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory))
                {
                    Directory.CreateDirectory(ConfigDirectory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(groups, options);
                File.WriteAllText(GroupsConfigPath, json);

                _cachedGroups = groups;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error saving URL groups: {ex.Message}");
                throw;
            }
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
        /// Imports built-in groups (adds them if they don't exist)
        /// Note: This enables them when manually imported by user
        /// </summary>
        public static void ImportBuiltInGroups()
        {
            var groups = LoadGroups();
            var builtInGroups = GetBuiltInGroups();

            foreach (var builtIn in builtInGroups)
            {
                if (!groups.Any(g => g.Id == builtIn.Id))
                {
                    builtIn.IsEnabled = true; // Enable when user manually imports
                    groups.Add(builtIn);
                }
            }

            SaveGroups(groups);
        }

        /// <summary>
        /// Ensures built-in groups exist in the config (auto-imported as DISABLED)
        /// Called automatically when app starts - user can enable as needed
        /// </summary>
        public static void EnsureBuiltInGroupsExist()
        {
            var groups = LoadGroups();
            var builtInGroups = GetBuiltInGroups();
            bool changed = false;

            foreach (var builtIn in builtInGroups)
            {
                if (!groups.Any(g => g.Id == builtIn.Id))
                {
                    builtIn.IsEnabled = false; // DISABLED by default for auto-import
                    groups.Add(builtIn);
                    changed = true;
                    Logger.Log($"Auto-imported built-in group '{builtIn.Name}' as disabled");
                }
            }

            if (changed)
            {
                SaveGroups(groups);
            }
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

            try
            {
                if (File.Exists(OverridesConfigPath))
                {
                    var json = File.ReadAllText(OverridesConfigPath);
                    _cachedOverrides = JsonSerializer.Deserialize<List<UrlGroupOverride>>(json) ?? new List<UrlGroupOverride>();
                    return _cachedOverrides;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading URL group overrides: {ex.Message}");
            }

            _cachedOverrides = new List<UrlGroupOverride>();
            return _cachedOverrides;
        }

        /// <summary>
        /// Saves all URL group overrides to the config file
        /// </summary>
        public static void SaveOverrides(List<UrlGroupOverride> overrides)
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory))
                {
                    Directory.CreateDirectory(ConfigDirectory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(overrides, options);
                File.WriteAllText(OverridesConfigPath, json);

                _cachedOverrides = overrides;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error saving URL group overrides: {ex.Message}");
                throw;
            }
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
        private static bool MatchesGroupPatterns(string url, List<string> patterns)
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

            foreach (var pattern in patterns)
            {
                string patternLower = pattern.ToLower();

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

        #endregion
    }
}
