using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using BrowserSelector.Models;

namespace BrowserSelector.Services
{
    /// <summary>
    /// Manages built-in URL group templates, including loading from embedded resources
    /// and applying updates without overwriting user preferences.
    /// </summary>
    public static class BuiltInTemplateManager
    {
        private static BuiltInTemplateManifest? _cachedManifest;

        /// <summary>
        /// Loads the built-in template manifest from embedded resources.
        /// </summary>
        public static BuiltInTemplateManifest LoadTemplateManifest()
        {
            if (_cachedManifest != null)
                return _cachedManifest;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("builtin-templates.json"));

                if (resourceName == null)
                {
                    Logger.Log("Warning: builtin-templates.json not found in embedded resources");
                    return new BuiltInTemplateManifest();
                }

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    Logger.Log("Warning: Could not load builtin-templates.json stream");
                    return new BuiltInTemplateManifest();
                }

                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                _cachedManifest = JsonSerializer.Deserialize<BuiltInTemplateManifest>(json, options)
                    ?? new BuiltInTemplateManifest();

                Logger.Log($"Loaded built-in template manifest v{_cachedManifest.Version} with {_cachedManifest.Templates.Count} templates");
                return _cachedManifest;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading template manifest: {ex.Message}");
                return new BuiltInTemplateManifest();
            }
        }

        /// <summary>
        /// Gets the currently installed template version from settings.
        /// </summary>
        public static string GetInstalledVersion()
        {
            return SettingsManager.LoadSettings().InstalledTemplateVersion;
        }

        /// <summary>
        /// Checks if template updates are needed and applies them.
        /// Call this on app startup.
        /// </summary>
        public static void ApplyTemplateUpdates()
        {
            try
            {
                var manifest = LoadTemplateManifest();
                var installedVersion = GetInstalledVersion();

                // Check if we need to apply updates
                bool needsUpdate = string.IsNullOrEmpty(installedVersion) ||
                                   CompareVersions(installedVersion, manifest.Version) < 0;

                if (!needsUpdate)
                {
                    Logger.Log($"Templates are up to date (v{installedVersion})");
                    return;
                }

                Logger.Log($"Applying template updates: v{installedVersion} -> v{manifest.Version}");

                var groups = UrlGroupManager.LoadGroups();
                bool changed = false;

                foreach (var template in manifest.Templates)
                {
                    var existingGroup = groups.FirstOrDefault(g => g.Id == template.Id);

                    if (existingGroup == null)
                    {
                        // Case 1: New group - create it (disabled by default)
                        var newGroup = CreateGroupFromTemplate(template);
                        newGroup.IsEnabled = false; // Disabled by default for auto-import
                        groups.Add(newGroup);
                        changed = true;
                        Logger.Log($"Added new built-in group: {template.Name}");
                    }
                    else
                    {
                        // Case 2: Existing group - merge patterns without removing user's
                        bool groupChanged = MergePatterns(existingGroup, template, installedVersion);
                        if (groupChanged)
                        {
                            changed = true;
                            Logger.Log($"Updated patterns in group: {template.Name}");
                        }
                    }
                }

                if (changed)
                {
                    UrlGroupManager.SaveGroups(groups);
                }

                // Update installed version
                var settings = SettingsManager.LoadSettings();
                settings.InstalledTemplateVersion = manifest.Version;
                SettingsManager.SaveSettings(settings);

                Logger.Log($"Template updates complete. Now at v{manifest.Version}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error applying template updates: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a new UrlGroup from a template definition.
        /// </summary>
        private static UrlGroup CreateGroupFromTemplate(BuiltInTemplate template)
        {
            var group = new UrlGroup
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                IsBuiltIn = true,
                IsEnabled = true,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                UrlPatterns = template.Patterns.Select(p => new UrlPattern
                {
                    Id = p.Id,
                    Pattern = p.Pattern,
                    IsBuiltIn = true,
                    VersionAdded = p.VersionAdded,
                    CreatedDate = DateTime.Now
                }).ToList()
            };

            return group;
        }

        /// <summary>
        /// Merges template patterns into an existing group without removing user patterns.
        /// </summary>
        private static bool MergePatterns(UrlGroup group, BuiltInTemplate template, string installedVersion)
        {
            bool changed = false;

            foreach (var templatePattern in template.Patterns)
            {
                // Skip if user has deleted this pattern
                if (group.DeletedBuiltInPatternIds.Contains(templatePattern.Id))
                {
                    continue;
                }

                // Check if pattern already exists (by ID)
                var existingPattern = group.UrlPatterns.FirstOrDefault(p => p.Id == templatePattern.Id);

                if (existingPattern == null)
                {
                    // Pattern doesn't exist - check if it's new in this version
                    bool isNewPattern = string.IsNullOrEmpty(installedVersion) ||
                                        CompareVersions(installedVersion, templatePattern.VersionAdded) < 0;

                    if (isNewPattern)
                    {
                        // Add the new pattern
                        group.UrlPatterns.Add(new UrlPattern
                        {
                            Id = templatePattern.Id,
                            Pattern = templatePattern.Pattern,
                            IsBuiltIn = true,
                            VersionAdded = templatePattern.VersionAdded,
                            CreatedDate = DateTime.Now
                        });
                        changed = true;
                        Logger.Log($"  Added pattern: {templatePattern.Pattern}");
                    }
                }
                else if (existingPattern.Pattern != templatePattern.Pattern)
                {
                    // Pattern exists but text changed - update it
                    existingPattern.Pattern = templatePattern.Pattern;
                    existingPattern.UpdatedDate = DateTime.Now;
                    changed = true;
                    Logger.Log($"  Updated pattern: {templatePattern.Pattern}");
                }
            }

            if (changed)
            {
                group.ModifiedDate = DateTime.Now;
            }

            return changed;
        }

        /// <summary>
        /// Compares two version strings (e.g., "1.0.0" vs "1.1.0").
        /// Returns: -1 if v1 < v2, 0 if equal, 1 if v1 > v2
        /// </summary>
        private static int CompareVersions(string v1, string v2)
        {
            if (string.IsNullOrEmpty(v1)) return -1;
            if (string.IsNullOrEmpty(v2)) return 1;

            try
            {
                var version1 = new Version(v1);
                var version2 = new Version(v2);
                return version1.CompareTo(version2);
            }
            catch
            {
                // Fallback to string comparison
                return string.Compare(v1, v2, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Clears the cached manifest (useful for testing).
        /// </summary>
        public static void ClearCache()
        {
            _cachedManifest = null;
        }
    }
}
