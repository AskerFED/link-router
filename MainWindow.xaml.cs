using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace BrowserSelector
{
    public partial class MainWindow : Window
    {
        private string _url = string.Empty;
        private bool _autoMatched = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadBrowsers();
        }

        public void SetUrl(string url)
        {
            _url = url;
            UrlTextBox.Text = url;

            // Try to auto-suggest pattern from URL
            try
            {
                var uri = new Uri(url);
                RulePatternTextBox.Text = uri.Host;
            }
            catch
            {
                RulePatternTextBox.Text = url;
            }

            // Check for matching rule
            CheckForMatchingRule();
        }

        private void CheckForMatchingRule()
        {
            var matchingRule = UrlRuleManager.FindMatchingRule(_url);
            if (matchingRule != null)
            {
                _autoMatched = true;

                // Find and select the matching browser
                foreach (BrowserInfo browser in BrowserComboBox.Items)
                {
                    if (browser.Name == matchingRule.BrowserName)
                    {
                        BrowserComboBox.SelectedItem = browser;

                        // Find and select the matching profile
                        foreach (ProfileInfo profile in ProfileComboBox.Items)
                        {
                            if (profile.Name == matchingRule.ProfileName)
                            {
                                ProfileComboBox.SelectedItem = profile;

                                // Auto-open if rule exists
                                OpenBrowser();
                                return;
                            }
                        }
                        break;
                    }
                }
            }
        }

        private void LoadBrowsers()
        {
            var browsers = BrowserDetector.GetInstalledBrowsers();
            BrowserComboBox.ItemsSource = browsers;

            if (browsers.Count > 0)
            {
                BrowserComboBox.SelectedIndex = 0;
            }
        }

        private void BrowserComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BrowserComboBox.SelectedItem is BrowserInfo browser)
            {
                var profiles = BrowserDetector.GetBrowserProfiles(browser);
                ProfileComboBox.ItemsSource = profiles;

                if (profiles.Count > 0)
                {
                    ProfileComboBox.SelectedIndex = 0;
                }
            }
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            // Save rule if checkbox is checked
            if (SaveRuleCheckBox.IsChecked == true && !string.IsNullOrWhiteSpace(RulePatternTextBox.Text))
            {
                SaveUrlRule();
            }

            OpenBrowser();
        }

        private void OpenBrowser()
        {
            if (BrowserComboBox.SelectedItem is BrowserInfo browser &&
                ProfileComboBox.SelectedItem is ProfileInfo profile)
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = browser.ExecutablePath,
                        UseShellExecute = false
                    };

                    if (!string.IsNullOrEmpty(profile.Arguments))
                    {
                        startInfo.Arguments = $"{profile.Arguments} \"{_url}\"";
                    }
                    else
                    {
                        startInfo.Arguments = $"\"{_url}\"";
                    }

                    Process.Start(startInfo);
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening browser: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a browser and profile.",
                    "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveUrlRule()
        {
            if (BrowserComboBox.SelectedItem is BrowserInfo browser &&
                ProfileComboBox.SelectedItem is ProfileInfo profile)
            {
                var rule = new UrlRule
                {
                    Pattern = RulePatternTextBox.Text.Trim(),
                    BrowserName = browser.Name,
                    BrowserPath = browser.ExecutablePath,
                    BrowserType = browser.Type,
                    ProfileName = profile.Name,
                    ProfilePath = profile.Path,
                    ProfileArguments = profile.Arguments
                };

                try
                {
                    UrlRuleManager.AddRule(rule);
                    MessageBox.Show($"Rule saved! URLs containing '{rule.Pattern}' will now open in {browser.Name} ({profile.Name})",
                        "Rule Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving rule: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ManageRulesButton_Click(object sender, RoutedEventArgs e)
        {
            var rulesWindow = new RulesManagerWindow();
            rulesWindow.Owner = this;
            rulesWindow.ShowDialog();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}