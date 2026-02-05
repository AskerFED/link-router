using System;
using System.IO;
using System.Windows;

namespace BrowserSelector
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                var url = e.Args[0];
                // Handle startup parameter (silent start)
                if (url.Equals("--startup", StringComparison.OrdinalIgnoreCase))
                {
                    // App started on Windows startup - just exit silently
                    // The app will be invoked again when a link is clicked
                    Shutdown();
                    return;
                }

                // Handle manage rules command
                if (url.Equals("--manage", StringComparison.OrdinalIgnoreCase))
                {
                    var settingsWindow = new SettingsWindow();
                    settingsWindow.ShowDialog();
                    Shutdown();
                    return;
                }

                // Handle register command - register and show settings with App tab
                if (url.Equals("--register", StringComparison.OrdinalIgnoreCase))
                {
                    RegistryHelper.RegisterAsDefaultBrowser();
                    var settingsWindow = new SettingsWindow();
                    settingsWindow.SelectAppTab();
                    settingsWindow.ShowDialog();
                    Shutdown();
                    return;
                }
                else if (url.Equals("--unregister", StringComparison.OrdinalIgnoreCase))
                {
                    RegistryHelper.UnregisterAsDefaultBrowser();
                    Shutdown();
                    return;
                }

                // Normal operation - show browser selector
                Logger.Log("Application_Startup fired with URL: " + url);
                var mainWindow = new MainWindow();
                mainWindow.SetUrl(url);
             }
            else
            {
                // No URL argument - show SettingsWindow directly
                Logger.Log("Application_Startup - No URL, showing SettingsWindow");
                var settingsWindow = new SettingsWindow();
                settingsWindow.ShowDialog();
                Shutdown();
            }
        }
    }
    public static class Logger
    {
        private static readonly string LogDirectory = @"D:\BrowserSelector";
        private static readonly string LogFilePath =
            Path.Combine(LogDirectory, "log.txt");

        public static void Log(string message)
        {
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                File.AppendAllText(
                    LogFilePath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}{Environment.NewLine}"
                );
            }
            catch
            {
                // Never crash the app because of logging
            }
        }
    }
}