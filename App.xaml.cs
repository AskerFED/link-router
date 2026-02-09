using System;
using System.IO;
using System.Windows;
using BrowserSelector.Services;

namespace BrowserSelector
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Logger.Log("========== APPLICATION STARTUP ==========");
            Logger.Log($"Arguments count: {e.Args.Length}");
            for (int i = 0; i < e.Args.Length; i++)
            {
                Logger.Log($"  Arg[{i}]: {e.Args[i]}");
            }

            // Pre-load profile avatars in background for better UX
            ProfileAvatarService.LoadAllAvatarsAtStartup();

            // Initialize built-in templates and apply any updates
            try
            {
                UrlGroupManager.EnsureBuiltInGroupsExist();
            }
            catch (Exception ex)
            {
                Logger.Log($"Warning: Failed to initialize built-in templates: {ex.Message}");
            }

            // Auto-register if not already registered
            if (!RegistryHelper.IsRegistered())
            {
                Logger.Log("App not registered - auto-registering...");
                try
                {
                    RegistryHelper.RegisterAsDefaultBrowser();
                    Logger.Log("Auto-registration completed successfully");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Auto-registration failed: {ex.Message}");
                }
            }

            if (e.Args.Length > 0)
            {
                var url = e.Args[0];
                Logger.Log($"Processing argument: {url}");

                // Handle startup parameter (silent start)
                if (url.Equals("--startup", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Log("Command: --startup (silent start) - exiting silently");
                    // App started on Windows startup - just exit silently
                    // The app will be invoked again when a link is clicked
                    Shutdown();
                    return;
                }

                // Handle manage rules command
                if (url.Equals("--manage", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Log("Command: --manage - opening SettingsWindow");
                    try
                    {
                        var settingsWindow = new SettingsWindow();
                        Logger.Log("SettingsWindow created, showing dialog...");
                        settingsWindow.ShowDialog();
                        Logger.Log("SettingsWindow closed");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"FATAL: Failed to create SettingsWindow: {ex}");
                        MessageBox.Show($"Error starting application:\n\n{ex.Message}\n\nCheck log at D:\\LinkRouter\\log.txt",
                            "LinkRouter Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    Logger.Log("Shutdown after --manage");
                    Shutdown();
                    return;
                }

                // Handle register command - register and show settings with App tab
                if (url.Equals("--register", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Log("Command: --register - registering as default browser");
                    try
                    {
                        Logger.Log("Calling RegistryHelper.RegisterAsDefaultBrowser()...");
                        RegistryHelper.RegisterAsDefaultBrowser();
                        Logger.Log("Registration complete, opening SettingsWindow");
                        var settingsWindow = new SettingsWindow();
                        settingsWindow.SelectAppTab();
                        Logger.Log("SettingsWindow created, showing dialog...");
                        settingsWindow.ShowDialog();
                        Logger.Log("SettingsWindow closed");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"FATAL: Failed to create SettingsWindow: {ex}");
                        MessageBox.Show($"Error starting application:\n\n{ex.Message}\n\nCheck log at D:\\LinkRouter\\log.txt",
                            "LinkRouter Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    Logger.Log("Shutdown after --register");
                    Shutdown();
                    return;
                }
                else if (url.Equals("--unregister", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Log("Command: --unregister - unregistering as default browser");
                    RegistryHelper.UnregisterAsDefaultBrowser();
                    Logger.Log("Unregistration complete, shutting down");
                    Shutdown();
                    return;
                }

                // Normal operation - process URL and open browser
                else
                {
                    Logger.Log("Normal operation - processing URL");

                    // Validate URL - treat empty/whitespace as no URL
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        Logger.Log("URL is empty/whitespace - treating as no URL, showing SettingsWindow");
                        try
                        {
                            var settingsWindow = new SettingsWindow();
                            settingsWindow.ShowDialog();
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"FATAL: Failed to create SettingsWindow: {ex}");
                            MessageBox.Show($"Error starting application:\n\n{ex.Message}",
                                "LinkRouter Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        Shutdown();
                        return;
                    }

                    Logger.Log($"Valid URL received: {url}");
                    Logger.Log("Creating MainWindow...");

                    MainWindow? mainWindow = null;
                    try
                    {
                        mainWindow = new MainWindow();
                        Logger.Log("MainWindow created successfully");
                        Logger.Log("Calling mainWindow.SetUrl()...");
                        mainWindow.SetUrl(url);
                        Logger.Log("SetUrl() completed - flow continues in MainWindow");
                        // Note: If SetUrl() finds a match and auto-opens, it calls Shutdown()
                        // If no match, HandleNoMatch() calls Show() on the window
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Error processing URL: {ex}");

                        // Try to show the window as fallback for manual selection
                        Logger.Log("Attempting fallback - showing window for manual selection");
                        try
                        {
                            if (mainWindow != null)
                            {
                                Logger.Log("MainWindow exists - showing it");
                                mainWindow.Show();
                                mainWindow.Activate();
                            }
                            else
                            {
                                // MainWindow creation failed - show error and shutdown
                                Logger.Log("MainWindow is null - showing error and shutting down");
                                MessageBox.Show($"Error processing URL:\n\n{ex.Message}",
                                    "LinkRouter Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                Shutdown();
                            }
                        }
                        catch (Exception fallbackEx)
                        {
                            // Complete failure - shutdown
                            Logger.Log($"Fallback also failed: {fallbackEx}");
                            MessageBox.Show($"Error starting application:\n\n{ex.Message}",
                                "LinkRouter Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            Shutdown();
                        }
                    }
                }
            }
            else
            {
                // No URL argument - show SettingsWindow directly
                Logger.Log("No arguments provided - showing SettingsWindow");
                try
                {
                    var settingsWindow = new SettingsWindow();
                    Logger.Log("SettingsWindow created, showing dialog...");
                    settingsWindow.ShowDialog();
                    Logger.Log("SettingsWindow closed");
                }
                catch (Exception ex)
                {
                    Logger.Log($"FATAL: Failed to create SettingsWindow: {ex}");
                    MessageBox.Show($"Error starting application:\n\n{ex.Message}\n\nCheck log at D:\\LinkRouter\\log.txt",
                        "LinkRouter Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                Logger.Log("Shutdown after SettingsWindow");
                Shutdown();
            }
        }

    }

    public static class Logger
    {
        private static readonly string LogDirectory = @"D:\LinkRouter";
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