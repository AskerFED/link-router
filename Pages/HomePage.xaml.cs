using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BrowserSelector.Services;

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
            // Hide dev-only features in production
            if (!AppConfig.DevMode)
            {
                TestAllRulesCard.Visibility = Visibility.Collapsed;
            }

            LoadData();
        }

        public void LoadData()
        {
            LoadDefaultBrowser();
            LoadStats();
        }

        private void LoadDefaultBrowser()
        {
            try
            {
                var browsers = BrowserService.GetBrowsersWithColors();
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

                // Auto-select first browser if none saved and browsers exist
                if (DefaultBrowserComboBox.SelectedItem == null && browsers.Count > 0)
                {
                    DefaultBrowserComboBox.SelectedItem = browsers[0];
                    // SelectionChanged handler will auto-save
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

                // Individual Rules stats
                RulesCountText.Text = rules.Count.ToString();
                int enabledRules = rules.Count(r => r.IsEnabled);
                int disabledRules = rules.Count(r => !r.IsEnabled);
                EnabledRulesCountText.Text = enabledRules.ToString();
                DisabledRulesCountText.Text = disabledRules.ToString();

                // URL Groups stats
                GroupsCountText.Text = groups.Count.ToString();
                int enabledGroups = groups.Count(g => g.IsEnabled);
                int disabledGroups = groups.Count(g => !g.IsEnabled);
                EnabledGroupsCountText.Text = enabledGroups.ToString();
                DisabledGroupsCountText.Text = disabledGroups.ToString();

                // Browsers stats
                BrowsersCountText.Text = browsers.Count.ToString();
            }
            catch (Exception ex)
            {
                Logger.Log($"HomePage LoadStats ERROR: {ex.Message}");
            }
        }

        private void DefaultBrowserComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DefaultBrowserComboBox.SelectedItem is BrowserInfoWithColor browserWithColor)
            {
                var browser = new BrowserInfo
                {
                    Name = browserWithColor.Name,
                    ExecutablePath = browserWithColor.ExecutablePath,
                    Type = browserWithColor.Type
                };
                DefaultBrowserManager.Save(browser);
                Logger.Log($"User changed default browser to: {browser.Name}");
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
