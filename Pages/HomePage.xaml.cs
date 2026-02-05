using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BrowserSelector.Pages
{
    public partial class HomePage : UserControl
    {
        public event EventHandler? NavigateToRulesRequested;
        public event EventHandler? TestAllRulesRequested;

        public HomePage()
        {
            InitializeComponent();
            Loaded += HomePage_Loaded;
        }

        private void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        public void LoadData()
        {
            LoadDefaultBrowser();
            LoadStats();
            UpdateRegistrationStatus();
        }

        private void LoadDefaultBrowser()
        {
            try
            {
                var browsers = BrowserDetector.GetInstalledBrowsers();
                DefaultBrowserComboBox.ItemsSource = browsers;

                var savedBrowser = DefaultBrowserManager.Load();
                if (savedBrowser != null)
                {
                    DefaultBrowserComboBox.SelectedItem =
                        browsers.FirstOrDefault(b =>
                            b.ExecutablePath.Equals(
                                savedBrowser.ExecutablePath,
                                StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"HomePage LoadDefaultBrowser ERROR: {ex.Message}");
            }
        }

        private void LoadStats()
        {
            try
            {
                var rules = UrlRuleManager.LoadRules();
                var groups = UrlGroupManager.LoadGroups();
                var browsers = BrowserDetector.GetInstalledBrowsers();

                RulesCountText.Text = rules.Count.ToString();
                GroupsCountText.Text = groups.Count.ToString();
                BrowsersCountText.Text = browsers.Count.ToString();
            }
            catch (Exception ex)
            {
                Logger.Log($"HomePage LoadStats ERROR: {ex.Message}");
            }
        }

        private void UpdateRegistrationStatus()
        {
            try
            {
                bool isRegistered = RegistryHelper.IsRegistered();

                if (isRegistered)
                {
                    RegistrationStatusText.Text = "Registered";
                    RegistrationStatusText.Foreground = new SolidColorBrush(Color.FromRgb(16, 124, 16));
                    RegistrationDetailText.Text = "LinkRouter is set up and ready to use";

                    // Update icon background
                    if (RegistrationIcon.Parent is Border iconBorder)
                    {
                        iconBorder.Background = new SolidColorBrush(Color.FromRgb(223, 246, 221));
                    }
                    RegistrationIcon.Stroke = new SolidColorBrush(Color.FromRgb(16, 124, 16));
                    RegistrationIcon.Data = (System.Windows.Media.Geometry)FindResource("CheckIcon");
                }
                else
                {
                    RegistrationStatusText.Text = "Not Registered";
                    RegistrationStatusText.Foreground = new SolidColorBrush(Color.FromRgb(196, 43, 28));
                    RegistrationDetailText.Text = "Register LinkRouter to start using it as your default browser handler";

                    // Update icon background
                    if (RegistrationIcon.Parent is Border iconBorder)
                    {
                        iconBorder.Background = new SolidColorBrush(Color.FromRgb(253, 231, 233));
                    }
                    RegistrationIcon.Stroke = new SolidColorBrush(Color.FromRgb(196, 43, 28));
                    RegistrationIcon.Data = (System.Windows.Media.Geometry)FindResource("CloseIcon");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"HomePage UpdateRegistrationStatus ERROR: {ex.Message}");
            }
        }

        private void DefaultBrowserComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DefaultBrowserComboBox.SelectedItem is BrowserInfo browser)
            {
                DefaultBrowserManager.Save(browser);
                Logger.Log($"User changed default browser to: {browser.Name}");
            }
        }

        private void OpenWindowsSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:defaultapps",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"OpenWindowsSettings_Click ERROR: {ex.Message}");
                MessageBox.Show("Could not open Windows Settings.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ManageRules_Click(object sender, RoutedEventArgs e)
        {
            NavigateToRulesRequested?.Invoke(this, EventArgs.Empty);
        }

        private void TestAllRules_Click(object sender, RoutedEventArgs e)
        {
            TestAllRulesRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
