using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using BrowserSelector.Services;

namespace BrowserSelector
{
    public class AppSettings
    {
        /// <summary>
        /// Schema version for data migration. Increment when making breaking changes.
        /// Version history:
        /// - 1: Initial version (implicit, pre-versioning)
        /// - 2: Added schema versioning, multi-profile rules
        /// </summary>
        public int SchemaVersion { get; set; } = 2;

        /// <summary>
        /// Application version that last saved these settings.
        /// </summary>
        public string AppVersion { get; set; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        /// <summary>
        /// Timestamp when settings were last saved.
        /// </summary>
        public DateTime LastSaved { get; set; } = DateTime.UtcNow;

        public bool IsEnabled { get; set; } = true;
        public string DefaultBrowserName { get; set; } = string.Empty;
        public string DefaultBrowserPath { get; set; } = string.Empty;
        public string DefaultBrowserType { get; set; } = string.Empty;
        public bool ShowNotifications { get; set; } = true;
        public bool UseLastActiveBrowser { get; set; } = true;
        public string LastActiveBrowserName { get; set; } = string.Empty;
        public string LastActiveBrowserPath { get; set; } = string.Empty;
        public string LastActiveBrowserType { get; set; } = string.Empty;
        public string LastActiveProfileName { get; set; } = string.Empty;
        public string LastActiveProfileArguments { get; set; } = string.Empty;
        public DateTime LastActiveTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Last selected navigation page (Home, Rules, Settings, Docs)
        /// </summary>
        public string LastSelectedPage { get; set; } = "Home";

        /// <summary>
        /// When enabled, automatically detect Windows account and apply
        /// matching browser profiles to the M365 URL group.
        /// </summary>
        public bool AutoDetectM365Profile { get; set; } = false;

        /// <summary>
        /// When true, overwrite existing M365 group configuration.
        /// When false (default), only apply if the group has no profiles configured.
        /// </summary>
        public bool AutoDetectOverwriteExisting { get; set; } = false;

        /// <summary>
        /// Cached detected Windows account email (for display).
        /// </summary>
        public string DetectedAccountEmail { get; set; } = string.Empty;

        /// <summary>
        /// Cached count of matched profiles.
        /// </summary>
        public int DetectedProfileCount { get; set; } = 0;

        /// <summary>
        /// Version of the built-in templates that have been applied.
        /// Used to determine if template updates need to be applied on app startup.
        /// </summary>
        public string InstalledTemplateVersion { get; set; } = string.Empty;
    }

    public static class SettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(
            AppConfig.AppDataFolder,
            "settings.json"
        );

        private static AppSettings? _cachedSettings = null;

        /// <summary>
        /// Current schema version. Increment when making breaking changes to data format.
        /// Version history:
        /// - 1: Initial version (implicit, pre-versioning)
        /// - 2: Added schema versioning, multi-profile rules
        /// - 3: Added UrlPattern objects with metadata, template versioning
        /// </summary>
        public const int CurrentSchemaVersion = 3;

        public static AppSettings LoadSettings()
        {
            if (_cachedSettings != null)
                return _cachedSettings;

            _cachedSettings = JsonStorageService.Load<AppSettings>(SettingsPath);

            // Check for schema version migrations
            if (_cachedSettings.SchemaVersion < CurrentSchemaVersion)
            {
                MigrateSettings(_cachedSettings);
            }

            return _cachedSettings;
        }

        /// <summary>
        /// Performs schema migrations based on version number.
        /// </summary>
        private static void MigrateSettings(AppSettings settings)
        {
            // Create backup before migration
            JsonStorageService.CreatePreMigrationBackup(SettingsPath, $"v{settings.SchemaVersion}-to-v{CurrentSchemaVersion}");

            // Version 0/1 -> 2: Add schema versioning (no data changes needed)
            if (settings.SchemaVersion < 2)
            {
                Logger.Log($"Migrating settings from schema v{settings.SchemaVersion} to v2");
                settings.SchemaVersion = 2;
            }

            // Version 2 -> 3: Added UrlPattern objects with metadata, template versioning
            // Note: URL group pattern migration is handled in UrlGroupManager.LoadGroups()
            if (settings.SchemaVersion < 3)
            {
                Logger.Log($"Migrating settings from schema v{settings.SchemaVersion} to v3");
                settings.SchemaVersion = 3;
                // InstalledTemplateVersion will be set by BuiltInTemplateManager on first run
            }

            // Save after all migrations
            if (settings.SchemaVersion == CurrentSchemaVersion)
            {
                SaveSettings(settings);
            }
        }

        public static void SaveSettings(AppSettings settings)
        {
            // Update metadata before saving
            settings.LastSaved = DateTime.UtcNow;
            settings.AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

            JsonStorageService.Save(SettingsPath, settings);
            _cachedSettings = settings;
        }

        public static void ClearCache()
        {
            _cachedSettings = null;
        }

        public static void UpdateLastActiveBrowser(BrowserInfo browser, ProfileInfo profile)
        {
            var settings = LoadSettings();
            settings.LastActiveBrowserName = browser.Name;
            settings.LastActiveBrowserPath = browser.ExecutablePath;
            settings.LastActiveBrowserType = browser.Type;
            settings.LastActiveProfileName = profile.Name;
            settings.LastActiveProfileArguments = profile.Arguments;
            settings.LastActiveTime = DateTime.Now;
            SaveSettings(settings);
        }

        public static bool HasRecentLastActiveBrowser()
        {
            var settings = LoadSettings();
            if (string.IsNullOrEmpty(settings.LastActiveBrowserPath))
                return false;

            // Consider "recent" if within the last 24 hours
            return DateTime.Now - settings.LastActiveTime < TimeSpan.FromHours(24);
        }
    }
}