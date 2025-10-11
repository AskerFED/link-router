using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace BrowserSelector
{
    public static class RegistryHelper
    {
        private const string AppName = "BrowserSelector";
        private const string AppDescription = "Browser Selector - Choose browser and profile";

        public static void RegisterAsDefaultBrowser()
        {
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
}