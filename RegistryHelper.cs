using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace BrowserSelector
{
    public static class RegistryHelper
    {
        public const string AppName = "LinkRouter";
        private const string AppDescription = "LinkRouter - Browser selection and URL routing utility";

        public static string? GetRegisteredAppName()
        {
            const string regPath = @"SOFTWARE\RegisteredApplications";

            using var key = Registry.LocalMachine.OpenSubKey(regPath);
            if (key == null) return null;

            foreach (var name in key.GetValueNames())
            {
                var value = key.GetValue(name)?.ToString();
            //    if(value!=null) Logger.Log(value);
                if (value != null && value.Contains(AppName, StringComparison.OrdinalIgnoreCase))
                {
                    return name; // This is the registeredApp name
                }
            }

            return null;
        }

        public static void RegisterAsDefaultBrowser()
        {
            Logger.Log("Register fired");
            DefaultBrowserRegistrar.EnsureDefaultBrowserRegistered();
            var exePath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");

            if (!File.Exists(exePath))
            {
                throw new FileNotFoundException("Executable not found", exePath);
            }

            try
            {
                // Register capabilities
                using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Clients\StartMenuInternet\{AppName}"))
                {
                    if (key != null)
                    {
                        key.SetValue("", AppDescription);

                        using (var defaultIconKey = key.CreateSubKey("DefaultIcon"))
                        {
                            if (defaultIconKey != null)
                            {
                                defaultIconKey.SetValue("", $"{exePath},0");
                            }
                        }

                        using (var shellKey = key.CreateSubKey(@"shell\open\command"))
                        {
                            if (shellKey != null)
                            {
                                shellKey.SetValue("", $"\"{exePath}\" \"%1\"");
                            }
                        }
                    }
                }

                // Register application
                using (var key = Registry.CurrentUser.CreateSubKey($@"Software\{AppName}"))
                {
                    if (key != null)
                    {
                        key.SetValue("", AppDescription);
                    }
                }

                // Register URL associations
                using (var capabilitiesKey = Registry.CurrentUser.CreateSubKey($@"Software\{AppName}\Capabilities"))
                {
                    if (capabilitiesKey != null)
                    {
                        capabilitiesKey.SetValue("ApplicationName", AppName);
                        capabilitiesKey.SetValue("ApplicationDescription", AppDescription);
                        capabilitiesKey.SetValue("ApplicationIcon", $"{exePath},0");

                        using (var urlAssocKey = capabilitiesKey.CreateSubKey("URLAssociations"))
                        {
                            if (urlAssocKey != null)
                            {
                                urlAssocKey.SetValue("http", $"{AppName}");
                                urlAssocKey.SetValue("https", $"{AppName}");
                            }
                        }

                        using (var fileAssocKey = capabilitiesKey.CreateSubKey("FileAssociations"))
                        {
                            if (fileAssocKey != null)
                            {
                                fileAssocKey.SetValue(".htm", $"{AppName}");
                                fileAssocKey.SetValue(".html", $"{AppName}");
                            }
                        }

                        using (var startMenuKey = capabilitiesKey.CreateSubKey("Startmenu"))
                        {
                            if (startMenuKey != null)
                            {
                                startMenuKey.SetValue("StartMenuInternet", AppName);
                            }
                        }
                    }
                }

                // Register in RegisteredApplications
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\RegisteredApplications", true))
                {
                    if (key != null)
                    {
                        key.SetValue(AppName, $@"Software\{AppName}\Capabilities");
                    }
                }

                // Register URL handler
                RegisterUrlProtocol("http", exePath);
                RegisterUrlProtocol("https", exePath);

                // Add to startup
                AddToStartup(exePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to register: {ex.Message}", ex);
            }
        }

        private static void AddToStartup(string exePath)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        // Add startup entry that runs in background (no window)
                        key.SetValue(AppName, $"\"{exePath}\" --startup");
                    }
                }
            }
            catch
            {
                // Startup registration is optional, don't fail the whole registration
            }
        }

        private static void RemoveFromStartup()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(AppName, false);
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        private static void RegisterUrlProtocol(string protocol, string exePath)
        {
            using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{AppName}"))
            {
                if (key != null)
                {
                    key.SetValue("", $"URL:{protocol}");
                    key.SetValue("URL Protocol", "");

                    using (var defaultIconKey = key.CreateSubKey("DefaultIcon"))
                    {
                        if (defaultIconKey != null)
                        {
                            defaultIconKey.SetValue("", $"{exePath},0");
                        }
                    }

                    using (var commandKey = key.CreateSubKey(@"shell\open\command"))
                    {
                        if (commandKey != null)
                        {
                            commandKey.SetValue("", $"\"{exePath}\" \"%1\"");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if LinkRouter is the system default browser (UserChoice points to us)
        /// </summary>
        public static bool IsSystemDefaultBrowser()
        {
            try
            {
                // Windows 11 2025+ uses UserChoiceLatest, fall back to UserChoice for older versions
                string? progId = null;

                // Try UserChoiceLatest\ProgId first (Windows 11 2025+ stores ProgId as a subkey)
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoiceLatest\ProgId"))
                {
                    progId = key?.GetValue("ProgId")?.ToString();
                    if (!string.IsNullOrEmpty(progId))
                    {
                        Logger.Log($"IsSystemDefaultBrowser check (UserChoiceLatest\\ProgId) - ProgId: '{progId}', AppName: '{AppName}'");
                    }
                }

                // Fall back to UserChoice if UserChoiceLatest not found
                if (string.IsNullOrEmpty(progId))
                {
                    using var key = Registry.CurrentUser.OpenSubKey(
                        @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice");
                    progId = key?.GetValue("ProgId")?.ToString();
                    Logger.Log($"IsSystemDefaultBrowser check (UserChoice) - ProgId: '{progId}', AppName: '{AppName}'");
                }

                var isDefault = string.Equals(progId, AppName, StringComparison.OrdinalIgnoreCase);
                Logger.Log($"IsSystemDefaultBrowser result: {isDefault}");

                return isDefault;
            }
            catch (Exception ex)
            {
                Logger.Log($"IsSystemDefaultBrowser ERROR: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if the application is registered as a browser handler
        /// </summary>
        public static bool IsRegistered()
        {
            try
            {
                // Check both old name and new name for compatibility
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey($@"Software\Clients\StartMenuInternet\{AppName}"))
                {
                    if (key != null)
                    {
                        using (RegistryKey? commandKey = key.OpenSubKey(@"shell\open\command"))
                        {
                            if (commandKey != null)
                            {
                                string? registeredPath = commandKey.GetValue("")?.ToString();
                                if (!string.IsNullOrEmpty(registeredPath) &&
                                    registeredPath.Contains("LinkRouter.exe"))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Clients\StartMenuInternet\LinkRouter"))
                {
                    if (key != null)
                    {
                        using (RegistryKey? commandKey = key.OpenSubKey(@"shell\open\command"))
                        {
                            if (commandKey != null)
                            {
                                string? registeredPath = commandKey.GetValue("")?.ToString();
                                return !string.IsNullOrEmpty(registeredPath) && registeredPath.Contains("LinkRouter.exe");
                            }
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static void UnregisterAsDefaultBrowser()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree($@"Software\Clients\StartMenuInternet\{AppName}", false);
                Registry.CurrentUser.DeleteSubKeyTree($@"Software\{AppName}", false);
                Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{AppName}", false);

                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\RegisteredApplications", true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(AppName, false);
                    }
                }

                RemoveFromStartup();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to unregister: {ex.Message}", ex);
            }
        }
    }
    public static class SystemDefaultBrowserDetector
    {
        public static BrowserInfo Get()
        {
            try
            {
                return BrowserDetector.MatchBrowserByPath(GetSystemDefaultBrowser().ExecutablePath);
            }
            catch
            {
                return null;
            }
        }
        public static BrowserInfo GetSystemDefaultBrowser()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice");

                var progId = key?.GetValue("ProgId")?.ToString();

                Logger.Log($"System default browser ProgId: {progId}");

                if (string.IsNullOrEmpty(progId))
                    return null;

                return ResolveProgIdToExe(progId);
            }
            catch (Exception ex)
            {
                Logger.Log($"Default browser detection failed: {ex}");
                return null;
            }
        }

        private static BrowserInfo ResolveProgIdToExe(string progId)
        {
            using var key = Registry.ClassesRoot.OpenSubKey(
                $@"{progId}\shell\open\command");

            var command = key?.GetValue(null)?.ToString();

            if (string.IsNullOrEmpty(command))
                return null;

            var exePath = ExtractExePath(command);

            if (!File.Exists(exePath))
                return null;

            return new BrowserInfo
            {
                Name = progId,
                ExecutablePath = exePath
            };
        }

        private static string ExtractExePath(string command)
        {
            command = command.Trim();

            if (command.StartsWith("\""))
            {
                return command.Substring(1, command.IndexOf("\"", 1) - 1);
            }

            return command.Split(' ')[0];
        }
    }
    public static class DefaultBrowserRegistrar
    {
        public static void EnsureDefaultBrowserRegistered()
        {
            var existing = DefaultBrowserManager.Load();
            if (existing != null)
            {
                Logger.Log($"Default browser already set: {existing.Name}");
                return;
            }

            Logger.Log("No default browser in registry. Resolving one.");

            var systemDefault = SystemDefaultBrowserDetector.Get();
            if (systemDefault != null)
            {
                DefaultBrowserManager.Save(systemDefault);
                Logger.Log($"Saved system default browser: {systemDefault.Name}");
                return;
            }

            var browsers = BrowserDetector.GetInstalledBrowsers();
            if (browsers.Count > 0)
            {
                DefaultBrowserManager.Save(browsers[0]);
                Logger.Log($"Saved fallback browser: {browsers[0].Name}");
            }
        }
    }
   
        public static class DefaultBrowserManager
        {
            private const string RegistryPath = @"Software\LinkRouter";
            private const string BrowserNameKey = "DefaultBrowserName";
            private const string BrowserExeKey = "DefaultBrowserExe";

            /// <summary>
            /// Save user-selected default browser to registry
            /// </summary>
            public static void Save(BrowserInfo browser)
            {
                if (browser == null)
                {
                    Logger.Log("DefaultBrowserManager.Save called with null browser");
                    return;
                }

                try
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                    {
                        key.SetValue(BrowserNameKey, browser.Name);
                        key.SetValue(BrowserExeKey, browser.ExecutablePath);
                    }

                    Logger.Log($"Saved default browser: {browser.Name} ({browser.ExecutablePath})");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to save default browser: {ex}");
                }
            }

            /// <summary>
            /// Load default browser from registry
            /// </summary>
            public static BrowserInfo Load()
            {
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                    {
                        if (key == null)
                        {
                            Logger.Log("Default browser registry key not found");
                            return null;
                        }

                        var name = key.GetValue(BrowserNameKey) as string;
                        var exePath = key.GetValue(BrowserExeKey) as string;

                        if (string.IsNullOrWhiteSpace(name) ||
                            string.IsNullOrWhiteSpace(exePath))
                        {
                            Logger.Log("Default browser registry values are missing");
                            return null;
                        }

                        Logger.Log($"Loaded default browser: {name} ({exePath})");

                        return new BrowserInfo
                        {
                            Name = name,
                            ExecutablePath = exePath
                        };
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to load default browser: {ex}");
                    return null;
                }
            }

            /// <summary>
            /// Check if a default browser is saved
            /// </summary>
            public static bool HasDefault()
            {
                return Load() != null;
            }

            /// <summary>
            /// Remove saved default browser
            /// </summary>
            public static void Clear()
            {
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(RegistryPath, false);
                    Logger.Log("Cleared default browser registry entry");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to clear default browser: {ex}");
                }
            }
        }
}