using System.Collections.Generic;
using System.Linq;

namespace BrowserSelector.Services
{
    /// <summary>
    /// Centralized service for browser detection and color mapping.
    /// Eliminates duplication of browser color dictionaries across windows.
    /// </summary>
    public static class BrowserService
    {
        /// <summary>
        /// Browser color mapping for UI display
        /// </summary>
        public static readonly Dictionary<string, string> BrowserColors = new Dictionary<string, string>
        {
            { "Google Chrome", "#4285F4" },
            { "Microsoft Edge", "#0078D7" },
            { "Mozilla Firefox", "#FF7139" },
            { "Brave Browser", "#FB542B" },
            { "Opera", "#FF1B2D" },
            { "Opera GX", "#FF006C" },
            { "Vivaldi", "#EF3939" },
            { "Arc", "#5B5FC7" }
        };

        /// <summary>
        /// Gets the color for a browser name
        /// </summary>
        public static string GetBrowserColor(string browserName)
        {
            return BrowserColors.TryGetValue(browserName, out var color) ? color : "#666666";
        }

        /// <summary>
        /// Gets all installed browsers with color and icon information
        /// </summary>
        public static List<BrowserInfoWithColor> GetBrowsersWithColors()
        {
            var browsers = BrowserDetector.GetInstalledBrowsers();
            return browsers.Select(b => new BrowserInfoWithColor
            {
                Name = b.Name,
                ExecutablePath = b.ExecutablePath,
                Type = b.Type,
                Color = GetBrowserColor(b.Name),
                Icon = BrowserIconService.GetBrowserIcon(b.ExecutablePath)
            }).ToList();
        }

        /// <summary>
        /// Gets profiles for a specific browser
        /// </summary>
        public static List<ProfileInfo> GetProfiles(BrowserInfoWithColor browser)
        {
            var browserInfo = new BrowserInfo
            {
                Name = browser.Name,
                ExecutablePath = browser.ExecutablePath,
                Type = browser.Type
            };
            return BrowserDetector.GetBrowserProfiles(browserInfo);
        }

        /// <summary>
        /// Gets profiles for a browser by path
        /// </summary>
        public static List<ProfileInfo> GetProfiles(string browserPath)
        {
            var browsers = BrowserDetector.GetInstalledBrowsers();
            var browser = browsers.FirstOrDefault(b => b.ExecutablePath == browserPath);
            if (browser == null) return new List<ProfileInfo>();
            return BrowserDetector.GetBrowserProfiles(browser);
        }

        /// <summary>
        /// Finds a browser by executable path
        /// </summary>
        public static BrowserInfoWithColor FindBrowserByPath(string browserPath)
        {
            var browsers = GetBrowsersWithColors();
            return browsers.FirstOrDefault(b => b.ExecutablePath == browserPath);
        }

        /// <summary>
        /// Finds a profile by path within a browser
        /// </summary>
        public static ProfileInfo FindProfileByPath(BrowserInfoWithColor browser, string profilePath)
        {
            var profiles = GetProfiles(browser);
            return profiles.FirstOrDefault(p => p.Path == profilePath);
        }
    }
}
