using System;
using System.IO;

namespace BrowserSelector
{
    /// <summary>
    /// Application configuration constants.
    /// </summary>
    public static class AppConfig
    {
        /// <summary>
        /// Application name used for folder names, registry keys, etc.
        /// </summary>
        public const string AppName = "LinkRouter";

        /// <summary>
        /// Path to the user data folder (%APPDATA%\LinkRouter)
        /// </summary>
        public static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppName);

        /// <summary>
        /// When true, shows development-only UI elements:
        /// - Documentation navigation item
        /// - Test All Rules buttons
        /// Set automatically based on build configuration.
        /// </summary>
#if DEBUG
        public const bool DevMode = true;
#else
        public const bool DevMode = false;
#endif
    }
}
