using System;
using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;

namespace BrowserSelector
{
    public partial class InfoWindow : Window
    {
        public InfoWindow()
        {
            InitializeComponent();
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool isRegistered = IsApplicationRegistered();

            // Show/Hide buttons based on registration status
            RegisterButton.Visibility = isRegistered ? Visibility.Collapsed : Visibility.Visible;
            UnregisterButton.Visibility = isRegistered ? Visibility.Visible : Visibility.Collapsed;

            // Update the registration info text
            UpdateRegistrationInfo(isRegistered);
        }

        private bool IsApplicationRegistered()
        {
            try
            {
                // Check if the application is registered in the registry
                string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Clients\StartMenuInternet\BrowserSelector"))
                {
                    if (key == null)
                        return false;

                    // Verify the executable path matches
                    using (RegistryKey commandKey = key.OpenSubKey(@"shell\open\command"))
                    {
                        if (commandKey != null)
                        {
                            string registeredPath = commandKey.GetValue("")?.ToString();
                            return !string.IsNullOrEmpty(registeredPath) && registeredPath.Contains("BrowserSelector.exe");
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

        private void UpdateRegistrationInfo(bool isRegistered)
        {
            if (isRegistered)
            {
                RegistrationStatusText.Text = "✅ Status: Registered";
                RegistrationStatusText.Foreground = System.Windows.Media.Brushes.Green;
                RegistrationDetailText.Text = "Browser Selector is registered and ready to use.\nSet it as default for http and https links in Windows Settings > Apps > Default apps.";
            }
            else
            {
                RegistrationStatusText.Text = "⚠️ Status: Not Registered";
                RegistrationStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                RegistrationDetailText.Text = "Click the Register button below to set up Browser Selector.";
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RegistryHelper.RegisterAsDefaultBrowser();

                MessageBox.Show(
                    "Browser Selector has been registered successfully!\n\n" +
                    "Features:\n" +
                    "✓ Added to Windows startup\n" +
                    "✓ Ready to intercept links\n\n" +
                    "To set it as default:\n" +
                    "1. Go to Windows Settings\n" +
                    "2. Apps > Default apps\n" +
                    "3. Search for 'Browser Selector'\n" +
                    "4. Set it as default for HTTP and HTTPS",
                    "Registration Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Update button states after registration
                UpdateButtonStates();

                // Ask if they want to manage rules
                var result = MessageBox.Show(
                    "Would you like to manage URL rules now?",
                    "Manage Rules",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var rulesWindow = new RulesManagerWindow();
                    rulesWindow.Owner = this;
                    rulesWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during registration: {ex.Message}",
                    "Registration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ManageRulesButton_Click(object sender, RoutedEventArgs e)
        {
            var rulesWindow = new RulesManagerWindow();
            rulesWindow.Owner = this;
            rulesWindow.ShowDialog();
        }

        private void UnregisterButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to unregister Browser Selector?\n\n" +
                "This will:\n" +
                "• Remove it from Windows startup\n" +
                "• Remove it as a browser option\n" +
                "• Keep your saved rules\n\n" +
                "You can re-register anytime.",
                "Confirm Unregister",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    RegistryHelper.UnregisterAsDefaultBrowser();

                    MessageBox.Show(
                        "Browser Selector has been unregistered and removed from startup.\n\n" +
                        "Your URL rules have been preserved.",
                        "Unregistration Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Update button states after unregistration
                    UpdateButtonStates();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during unregistration: {ex.Message}",
                        "Unregistration Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}