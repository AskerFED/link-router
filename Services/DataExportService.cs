using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace BrowserSelector.Services
{
    /// <summary>
    /// Handles export and import of all user data for backup/restore functionality.
    /// </summary>
    public static class DataExportService
    {
        private static readonly JsonSerializerOptions ExportOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Contains all exportable user data in a single package.
        /// </summary>
        public class ExportPackage
        {
            /// <summary>
            /// Export format version for forward compatibility.
            /// </summary>
            public int ExportVersion { get; set; } = 1;

            /// <summary>
            /// Application version that created this export.
            /// </summary>
            public string AppVersion { get; set; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

            /// <summary>
            /// When this export was created.
            /// </summary>
            public DateTime ExportDate { get; set; } = DateTime.UtcNow;

            /// <summary>
            /// Machine name where export was created (for identification).
            /// </summary>
            public string MachineName { get; set; } = Environment.MachineName;

            /// <summary>
            /// User settings (preferences, defaults).
            /// </summary>
            public AppSettings? Settings { get; set; }

            /// <summary>
            /// Individual URL rules.
            /// </summary>
            public List<UrlRule>? Rules { get; set; }

            /// <summary>
            /// URL groups configuration.
            /// </summary>
            public List<UrlGroup>? UrlGroups { get; set; }

            /// <summary>
            /// URL group overrides.
            /// </summary>
            public List<UrlGroupOverride>? UrlGroupOverrides { get; set; }
        }

        /// <summary>
        /// Result of an import operation.
        /// </summary>
        public class ImportResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public int SettingsImported { get; set; }
            public int RulesImported { get; set; }
            public int UrlGroupsImported { get; set; }
            public int OverridesImported { get; set; }
        }

        /// <summary>
        /// Exports all user data to a JSON file.
        /// </summary>
        public static void Export(string filePath)
        {
            try
            {
                // Clear caches to ensure fresh data
                SettingsManager.ClearCache();
                UrlRuleManager.ClearCache();
                UrlGroupManager.ClearGroupsCache();
                UrlGroupManager.ClearOverridesCache();

                var package = new ExportPackage
                {
                    Settings = SettingsManager.LoadSettings(),
                    Rules = UrlRuleManager.LoadRules(),
                    UrlGroups = UrlGroupManager.LoadGroups(),
                    UrlGroupOverrides = UrlGroupManager.LoadOverrides()
                };

                var json = JsonSerializer.Serialize(package, ExportOptions);
                File.WriteAllText(filePath, json);

                Logger.Log($"Exported data to: {filePath}");
                Logger.Log($"  Settings: 1, Rules: {package.Rules?.Count ?? 0}, " +
                          $"Groups: {package.UrlGroups?.Count ?? 0}, Overrides: {package.UrlGroupOverrides?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Export failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Imports user data from a JSON export file.
        /// Creates a backup of current data before importing.
        /// </summary>
        public static ImportResult Import(string filePath, bool replaceExisting = true)
        {
            var result = new ImportResult();

            try
            {
                // Read and parse the export file
                var json = File.ReadAllText(filePath);
                var package = JsonSerializer.Deserialize<ExportPackage>(json);

                if (package == null)
                {
                    result.Message = "Invalid export file format.";
                    return result;
                }

                Logger.Log($"Importing data from: {filePath}");
                Logger.Log($"  Export version: {package.ExportVersion}, App version: {package.AppVersion}");
                Logger.Log($"  Export date: {package.ExportDate:yyyy-MM-dd HH:mm:ss}");

                // Create full backup before import
                CreatePreImportBackup();

                // Clear caches
                SettingsManager.ClearCache();
                UrlRuleManager.ClearCache();
                UrlGroupManager.ClearGroupsCache();
                UrlGroupManager.ClearOverridesCache();

                // Import settings
                if (package.Settings != null)
                {
                    // Preserve certain local settings
                    var currentSettings = SettingsManager.LoadSettings();
                    package.Settings.LastSelectedPage = currentSettings.LastSelectedPage;

                    SettingsManager.SaveSettings(package.Settings);
                    result.SettingsImported = 1;
                }

                // Import rules
                if (package.Rules != null && package.Rules.Count > 0)
                {
                    if (replaceExisting)
                    {
                        UrlRuleManager.SaveRules(package.Rules);
                    }
                    else
                    {
                        // Merge - add rules that don't exist
                        var existingRules = UrlRuleManager.LoadRules();
                        foreach (var rule in package.Rules)
                        {
                            if (!existingRules.Exists(r => r.Pattern == rule.Pattern))
                            {
                                existingRules.Add(rule);
                            }
                        }
                        UrlRuleManager.SaveRules(existingRules);
                    }
                    result.RulesImported = package.Rules.Count;
                }

                // Import URL groups
                if (package.UrlGroups != null && package.UrlGroups.Count > 0)
                {
                    if (replaceExisting)
                    {
                        UrlGroupManager.SaveGroups(package.UrlGroups);
                    }
                    else
                    {
                        // Merge - add groups that don't exist
                        var existingGroups = UrlGroupManager.LoadGroups();
                        foreach (var group in package.UrlGroups)
                        {
                            if (!existingGroups.Exists(g => g.Id == group.Id || g.Name == group.Name))
                            {
                                existingGroups.Add(group);
                            }
                        }
                        UrlGroupManager.SaveGroups(existingGroups);
                    }
                    result.UrlGroupsImported = package.UrlGroups.Count;
                }

                // Import URL group overrides
                if (package.UrlGroupOverrides != null && package.UrlGroupOverrides.Count > 0)
                {
                    if (replaceExisting)
                    {
                        UrlGroupManager.SaveOverrides(package.UrlGroupOverrides);
                    }
                    else
                    {
                        // Merge - add overrides that don't exist
                        var existingOverrides = UrlGroupManager.LoadOverrides();
                        foreach (var over in package.UrlGroupOverrides)
                        {
                            if (!existingOverrides.Exists(o => o.Id == over.Id))
                            {
                                existingOverrides.Add(over);
                            }
                        }
                        UrlGroupManager.SaveOverrides(existingOverrides);
                    }
                    result.OverridesImported = package.UrlGroupOverrides.Count;
                }

                result.Success = true;
                result.Message = $"Import completed successfully.\n" +
                                $"Settings: {result.SettingsImported}\n" +
                                $"Rules: {result.RulesImported}\n" +
                                $"URL Groups: {result.UrlGroupsImported}\n" +
                                $"Overrides: {result.OverridesImported}";

                Logger.Log($"Import completed: {result.Message}");
            }
            catch (JsonException ex)
            {
                result.Message = $"Invalid JSON format: {ex.Message}";
                Logger.Log($"Import failed (JSON error): {ex.Message}");
            }
            catch (Exception ex)
            {
                result.Message = $"Import failed: {ex.Message}";
                Logger.Log($"Import failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Creates a backup of all current data before importing.
        /// </summary>
        private static void CreatePreImportBackup()
        {
            var configDir = AppConfig.AppDataFolder;
            var backupDir = Path.Combine(configDir, "backups");

            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var backupPath = Path.Combine(backupDir, $"pre-import_{timestamp}.json");

            try
            {
                // Export current data to backup
                var package = new ExportPackage
                {
                    Settings = SettingsManager.LoadSettings(),
                    Rules = UrlRuleManager.LoadRules(),
                    UrlGroups = UrlGroupManager.LoadGroups(),
                    UrlGroupOverrides = UrlGroupManager.LoadOverrides()
                };

                var json = JsonSerializer.Serialize(package, ExportOptions);
                File.WriteAllText(backupPath, json);

                Logger.Log($"Created pre-import backup: {backupPath}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Warning: Could not create pre-import backup: {ex.Message}");
                // Continue with import anyway
            }
        }

        /// <summary>
        /// Validates an export file without importing it.
        /// </summary>
        public static (bool valid, string message, ExportPackage? package) ValidateExportFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return (false, "File not found.", null);

                var json = File.ReadAllText(filePath);
                var package = JsonSerializer.Deserialize<ExportPackage>(json);

                if (package == null)
                    return (false, "Invalid export file format.", null);

                var summary = $"Export file information:\n" +
                             $"  App Version: {package.AppVersion}\n" +
                             $"  Export Date: {package.ExportDate:yyyy-MM-dd HH:mm:ss}\n" +
                             $"  Machine: {package.MachineName}\n\n" +
                             $"Data to import:\n" +
                             $"  Rules: {package.Rules?.Count ?? 0}\n" +
                             $"  URL Groups: {package.UrlGroups?.Count ?? 0}\n" +
                             $"  Overrides: {package.UrlGroupOverrides?.Count ?? 0}";

                return (true, summary, package);
            }
            catch (JsonException ex)
            {
                return (false, $"Invalid JSON format: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Error reading file: {ex.Message}", null);
            }
        }
    }
}
