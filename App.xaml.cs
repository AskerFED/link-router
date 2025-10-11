using System;
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
                    var rulesWindow = new RulesManagerWindow();
                    rulesWindow.ShowDialog();
                    Shutdown();
                    return;
                }

                // Handle register/unregister commands
                if (url.Equals("--register", StringComparison.OrdinalIgnoreCase))
                {
                    RegistryHelper.RegisterAsDefaultBrowser();

                    var result = MessageBox.Show(
                        "Browser Selector has been registered successfully!\n\n" +
                        "Features:\n" +
                        "✓ Added to Windows startup\n" +
                        "✓ Ready to intercept links\n\n" +
                        "To set it as default:\n" +
                        "1. Go to Windows Settings\n" +
                        "2. Apps > Default apps\n" +
                        "3. Search for 'Browser Selector'\n" +
                        "4. Set it as default for HTTP and HTTPS\n\n" +
                        "Would you like to manage URL rules now?",
                        "Registration Complete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        var rulesWindow = new RulesManagerWindow();
                        rulesWindow.ShowDialog();
                    }

                    Shutdown();
                    return;
                }
                else if (url.Equals("--unregister", StringComparison.OrdinalIgnoreCase))
                {
                    RegistryHelper.UnregisterAsDefaultBrowser();
                    MessageBox.Show("Browser Selector has been unregistered and removed from startup.",
                                  "Unregistration Complete",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                    Shutdown();
                    return;
                }

                // Normal operation - show browser selector
                var mainWindow = new MainWindow();
                mainWindow.SetUrl(url);
                mainWindow.Show();
            }
            else
            {
                // Show info window with manage button
                var infoWindow = new InfoWindow();
                infoWindow.ShowDialog();
                Shutdown();
            }
        }
    }
}