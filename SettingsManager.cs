using System;
using System.IO;
using System.Text.Json;

namespace BrowserSelector
{
    public class AppSettings
    {
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
    }

    public static class SettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BrowserSelector",
            "settings.json"
        );

        private static AppSettings? _cachedSettings = null;

        public static AppSettings LoadSettings()
        {
            if (_cachedSettings != null)
                return _cachedSettings;

            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    return _cachedSettings;
                }
            }
            catch
            {
                // If there's an error reading, start fresh
            }

            _cachedSettings = new AppSettings();
            return _cachedSettings;
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsPath, json);

                _cachedSettings = settings;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save settings: {ex.Message}", ex);
            }
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