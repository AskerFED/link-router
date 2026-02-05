using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BrowserSelector
{
    public partial class MainWindow : Window
    {
        private string _url = string.Empty;

        public MainWindow()
        {
            Logger.Log("MainWindow open");
            try
            {
                InitializeComponent();
                LoadBrowsers();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error initializing MainWindow: {ex}");
            }
            Logger.Log("MainWindow initialized");
        }

        public void SetUrl(string url)
        {
            _url = url;
            UrlTextBox.Text = url;

            // Check for matching rule
            CheckForMatchingRule();
        }

        private void CheckForMatchingRule()
        {
            // Use the enhanced FindMatch that checks individual rules and URL groups
            var match = UrlRuleManager.FindMatch(_url);

            switch (match.Type)
            {
                case MatchType.IndividualRule:
                    // Individual URL rule matched - check for multiple profiles
                    HandleIndividualRuleMatch(match);
                    break;

                case MatchType.GroupOverride:
                    // URL group override matched
                    HandleGroupMatch(match, isOverride: true);
                    break;

                case MatchType.UrlGroup:
                    // URL group matched
                    HandleGroupMatch(match, isOverride: false);
                    break;

                case MatchType.NoMatch:
                default:
                    // No match - show window for user to select
                    HandleNoMatch();
                    break;
            }
        }

        private void HandleIndividualRuleMatch(MatchResult match)
        {
            var rule = match.Rule;
            if (rule == null) return;

            // Check if rule has multiple profiles - show picker
            if (rule.HasMultipleProfiles)
            {
                Logger.Log($"Individual rule match with multiple profiles -> showing picker ({rule.Profiles.Count} profiles)");
                ShowRuleProfilePicker(rule);
                return;
            }

            // Single profile - auto open
            RuleProfile? ruleProfile = rule.FirstProfile;
            string browserName = ruleProfile?.BrowserName ?? rule.BrowserName;
            string profileName = ruleProfile?.ProfileName ?? rule.ProfileName;

            var browser = BrowserDetector
                .GetInstalledBrowsers()
                .FirstOrDefault(b => b.Name == browserName);

            var profile = browser == null
                ? null
                : BrowserDetector
                    .GetBrowserProfiles(browser)
                    .FirstOrDefault(p => p.Name == profileName);

            Logger.Log($"Individual rule match -> {browser?.Name} | {profile?.Name}");

            OpenBrowser(browser, profile);
            Application.Current.Shutdown();
        }

        private void ShowRuleProfilePicker(UrlRule rule)
        {
            // Show a picker window for multi-profile rules
            var pickerWindow = new RuleProfilePickerWindow(_url, rule);
            pickerWindow.ShowDialog();
            Application.Current.Shutdown();
        }

        private void ShowGroupProfilePicker(UrlGroup group)
        {
            // Show a picker window for multi-profile groups
            var pickerWindow = new RuleProfilePickerWindow(_url, group);
            pickerWindow.ShowDialog();
            Application.Current.Shutdown();
        }

        private void HandleGroupMatch(MatchResult match, bool isOverride)
        {
            var group = match.Group;
            if (group == null)
            {
                HandleNoMatch();
                return;
            }

            // Check if group has multiple profiles or ShowProfilePicker behavior
            if (!isOverride && (group.HasMultipleProfiles || group.Behavior == UrlGroupBehavior.ShowProfilePicker))
            {
                Logger.Log($"URL group match with multiple profiles -> showing picker ({group.Profiles?.Count ?? 0} profiles)");
                ShowGroupProfilePicker(group);
                return;
            }

            // Single profile or override - auto open with group's default browser/profile
            string browserName = match.GetBrowserName();
            string browserPath = match.GetBrowserPath();
            string profileName = match.GetProfileName();
            string profileArgs = match.GetProfileArguments();

            if (string.IsNullOrEmpty(browserName) || string.IsNullOrEmpty(browserPath))
            {
                Logger.Log($"URL group matched but no default browser configured");
                HandleNoMatch();
                return;
            }

            var browser = new BrowserInfo
            {
                Name = browserName,
                ExecutablePath = browserPath,
                Type = isOverride ? match.Override?.BrowserType ?? "" : match.Group?.DefaultBrowserType ?? ""
            };

            ProfileInfo? profile = null;
            if (!string.IsNullOrEmpty(profileName))
            {
                profile = new ProfileInfo
                {
                    Name = profileName,
                    Path = isOverride ? match.Override?.ProfilePath ?? "" : match.Group?.DefaultProfilePath ?? "",
                    Arguments = profileArgs
                };
            }

            Logger.Log($"URL group match (UseDefault) -> {browser.Name} | {profile?.Name}");

            OpenBrowser(browser, profile);
            Application.Current.Shutdown();
        }

        private void HandleNoMatch()
        {
            // Show this window to let user select browser/profile
            this.Show();
            this.Activate();
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

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Reserved for future use
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (BrowserComboBox.SelectedItem is BrowserInfo browser &&
                ProfileComboBox.SelectedItem is ProfileInfo profile)
            {
                // Save rule if checkbox is checked
                if (OpenUrlCheckBox.IsChecked == true)
                {
                    SaveUrlRule(browser, profile);
                }

                OpenBrowser(browser, profile);
                Application.Current.Shutdown();
            }
            else
            {
                MessageBox.Show("Please select a browser and profile.",
                    "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OpenBrowser(BrowserInfo browser, ProfileInfo profile, bool isDefault = false)
        {
            if (browser == null)
            {
                Logger.Log("OpenBrowser failed: browser is null");
                return;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = browser.ExecutablePath,
                    UseShellExecute = false
                };

                if (!isDefault && profile != null && !string.IsNullOrEmpty(profile.Arguments))
                {
                    Logger.Log($"Opening -> {browser.Name} | {profile.Name}");
                    startInfo.Arguments = $"{profile.Arguments} \"{_url}\"";
                }
                else
                {
                    Logger.Log($"Opening -> {browser.Name} (default profile)");
                    startInfo.Arguments = $"\"{_url}\"";
                }

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Logger.Log($"OpenBrowser error: {ex}");
                MessageBox.Show($"Failed to open browser: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveUrlRule(BrowserInfo browser, ProfileInfo profile)
        {
            try
            {
                // Extract domain from URL for the pattern
                string pattern;
                try
                {
                    var uri = new Uri(_url);
                    pattern = uri.Host;
                }
                catch
                {
                    pattern = _url;
                }

                var ruleProfile = new RuleProfile
                {
                    BrowserName = browser.Name,
                    BrowserPath = browser.ExecutablePath,
                    BrowserType = browser.Type,
                    ProfileName = profile.Name,
                    ProfilePath = profile.Path,
                    ProfileArguments = profile.Arguments,
                    DisplayOrder = 0
                };

                var rule = new UrlRule
                {
                    Pattern = pattern,
                    Profiles = new System.Collections.Generic.List<RuleProfile> { ruleProfile },
                    // Also set legacy fields for compatibility
                    BrowserName = browser.Name,
                    BrowserPath = browser.ExecutablePath,
                    BrowserType = browser.Type,
                    ProfileName = profile.Name,
                    ProfilePath = profile.Path,
                    ProfileArguments = profile.Arguments
                };

                UrlRuleManager.AddRule(rule);
                Logger.Log($"Rule saved: {pattern} -> {browser.Name}/{profile.Name}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error saving rule: {ex}");
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
