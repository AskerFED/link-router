using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DragMove();
            }
        }

        public void SetUrl(string url)
        {
            Logger.Log($"SetUrl called with: {url}");
            _url = url;
            UrlTextBox.Text = url;

            // Check for matching rule
            Logger.Log("Calling CheckForMatchingRule...");
            CheckForMatchingRule();
        }

        private void CheckForMatchingRule()
        {
            Logger.Log($"CheckForMatchingRule started for URL: {_url}");
            try
            {
                // Use the enhanced FindMatch that checks individual rules and URL groups
                Logger.Log("Calling UrlRuleManager.FindMatch...");
                var match = UrlRuleManager.FindMatch(_url);
                Logger.Log($"FindMatch returned: {match.Type}");

                switch (match.Type)
                {
                    case MatchType.IndividualRule:
                        // Individual URL rule matched - check for multiple profiles
                        Logger.Log($"Match type: IndividualRule - Pattern: {match.Rule?.Pattern}");
                        HandleIndividualRuleMatch(match);
                        break;

                    case MatchType.GroupOverride:
                        // URL group override matched
                        Logger.Log($"Match type: GroupOverride - Group: {match.Group?.Name}, Override: {match.Override?.UrlPattern}");
                        HandleGroupMatch(match, isOverride: true);
                        break;

                    case MatchType.UrlGroup:
                        // URL group matched
                        Logger.Log($"Match type: UrlGroup - Group: {match.Group?.Name}");
                        HandleGroupMatch(match, isOverride: false);
                        break;

                    case MatchType.NoMatch:
                    default:
                        // No match - use default browser or show window
                        Logger.Log("Match type: NoMatch - will try default browser or show window");
                        HandleNoMatch();
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in CheckForMatchingRule: {ex}");
                // Fallback to showing window for manual selection
                HandleNoMatch();
            }
        }

        private void HandleIndividualRuleMatch(MatchResult match)
        {
            Logger.Log("HandleIndividualRuleMatch called");
            var rule = match.Rule;
            if (rule == null)
            {
                Logger.Log("Rule is null - calling HandleNoMatch");
                HandleNoMatch();
                return;
            }

            Logger.Log($"Rule found - Pattern: {rule.Pattern}, HasMultipleProfiles: {rule.HasMultipleProfiles}");

            // Check if rule has multiple profiles - show picker
            if (rule.HasMultipleProfiles)
            {
                Logger.Log($"Rule has multiple profiles ({rule.Profiles.Count}) -> showing picker");
                ShowRuleProfilePicker(rule);
                return;
            }

            // Single profile - auto open
            Logger.Log("Rule has single profile - auto opening");
            RuleProfile? ruleProfile = rule.FirstProfile;
            string browserName = ruleProfile?.BrowserName ?? rule.BrowserName;
            string profileName = ruleProfile?.ProfileName ?? rule.ProfileName;
            Logger.Log($"Target browser: {browserName}, profile: {profileName}");

            Logger.Log("Getting installed browsers...");
            var browser = BrowserDetector
                .GetInstalledBrowsers()
                .FirstOrDefault(b => b.Name == browserName);

            // Check if browser was found - fallback to manual selection if not
            if (browser == null)
            {
                Logger.Log($"Browser not found: {browserName} - calling HandleNoMatch");
                HandleNoMatch();
                return;
            }

            Logger.Log($"Browser found: {browser.Name} ({browser.ExecutablePath})");
            Logger.Log("Getting browser profiles...");

            // Get the stable profile path from the rule for matching
            string profilePath = ruleProfile?.ProfilePath ?? rule.ProfilePath;
            var profiles = BrowserDetector.GetBrowserProfiles(browser);

            // Match by Path (stable) first, then fall back to Name
            var profile = profiles.FirstOrDefault(p => p.Path == profilePath)
                       ?? profiles.FirstOrDefault(p => p.Name == profileName);

            Logger.Log($"Profile result: {(profile != null ? profile.Name : "null (will use default)")}");
            Logger.Log($"Individual rule match complete -> Opening {browser.Name} | {profile?.Name}");

            OpenBrowser(browser, profile);
            Logger.Log("Calling Application.Shutdown()");
            Application.Current.Shutdown();
        }

        private void ShowRuleProfilePicker(UrlRule rule)
        {
            Logger.Log($"ShowRuleProfilePicker called for rule: {rule.Pattern}");
            // Show a picker window for multi-profile rules
            var pickerWindow = new RuleProfilePickerWindow(_url, rule);
            Logger.Log("Showing RuleProfilePickerWindow dialog...");
            pickerWindow.ShowDialog();
            Logger.Log("RuleProfilePickerWindow closed - shutting down");
            Application.Current.Shutdown();
        }

        private void ShowGroupProfilePicker(UrlGroup group)
        {
            Logger.Log($"ShowGroupProfilePicker called for group: {group.Name}");
            // Show a picker window for multi-profile groups
            var pickerWindow = new RuleProfilePickerWindow(_url, group);
            Logger.Log("Showing RuleProfilePickerWindow dialog (group mode)...");
            pickerWindow.ShowDialog();
            Logger.Log("RuleProfilePickerWindow closed - shutting down");
            Application.Current.Shutdown();
        }

        private void HandleGroupMatch(MatchResult match, bool isOverride)
        {
            Logger.Log($"HandleGroupMatch called - isOverride: {isOverride}");
            var group = match.Group;
            if (group == null)
            {
                Logger.Log("Group is null - calling HandleNoMatch");
                HandleNoMatch();
                return;
            }

            Logger.Log($"Group found: {group.Name}, Behavior: {group.Behavior}, HasMultipleProfiles: {group.HasMultipleProfiles}");

            // Check if group has multiple profiles or ShowProfilePicker behavior
            if (!isOverride && (group.HasMultipleProfiles || group.Behavior == UrlGroupBehavior.ShowProfilePicker))
            {
                Logger.Log($"Group requires picker ({group.Profiles?.Count ?? 0} profiles, Behavior: {group.Behavior}) -> showing picker");
                ShowGroupProfilePicker(group);
                return;
            }

            // Single profile or override - auto open with group's default browser/profile
            Logger.Log("Group has single profile or is override - auto opening");
            string browserName = match.GetBrowserName();
            string browserPath = match.GetBrowserPath();
            string profileName = match.GetProfileName();
            string profileArgs = match.GetProfileArguments();

            Logger.Log($"Group browser config - Name: {browserName}, Path: {browserPath}, Profile: {profileName}");

            if (string.IsNullOrEmpty(browserName) || string.IsNullOrEmpty(browserPath))
            {
                Logger.Log("Group has no default browser configured - calling HandleNoMatch");
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
                Logger.Log($"Profile configured: {profile.Name}");
            }
            else
            {
                Logger.Log("No profile configured - using default");
            }

            Logger.Log($"URL group match complete -> Opening {browser.Name} | {profile?.Name}");

            OpenBrowser(browser, profile);
            Logger.Log("Calling Application.Shutdown()");
            Application.Current.Shutdown();
        }

        private void HandleNoMatch()
        {
            Logger.Log("HandleNoMatch called - checking for default browser...");

            // Try to use saved default browser
            var defaultBrowser = DefaultBrowserManager.Load();
            Logger.Log($"DefaultBrowserManager.Load() returned: {(defaultBrowser != null ? defaultBrowser.Name : "null")}");

            if (defaultBrowser != null && !string.IsNullOrEmpty(defaultBrowser.ExecutablePath))
            {
                Logger.Log($"Default browser found: {defaultBrowser.Name} ({defaultBrowser.ExecutablePath})");
                Logger.Log($"Opening URL in default browser...");

                // Open URL in default browser (no profile)
                OpenBrowser(defaultBrowser, null, isDefault: true);

                // Show notification if enabled
                var settings = SettingsManager.LoadSettings();
                Logger.Log($"ShowNotifications setting: {settings.ShowNotifications}");

                if (settings.ShowNotifications)
                {
                    Logger.Log("Showing notification toast...");
                    NotificationHelper.ShowNotification(_url, () =>
                    {
                        // Callback for "Create Rule" button - show this window
                        Logger.Log("Notification 'Create Rule' clicked - showing MainWindow");
                        this.Show();
                        this.Activate();
                    });
                }
                else
                {
                    // No notification - just shutdown
                    Logger.Log("Notifications disabled - shutting down");
                    Application.Current.Shutdown();
                }
            }
            else
            {
                // No default browser configured - show window for manual selection
                Logger.Log("No default browser configured - showing selection window");
                this.Show();
                this.Activate();
            }
        }

        private void LoadBrowsers()
        {
            Logger.Log("LoadBrowsers called");
            var browsers = BrowserDetector.GetInstalledBrowsers();
            Logger.Log($"Found {browsers.Count} installed browsers");
            BrowserComboBox.ItemsSource = browsers;

            if (browsers.Count > 0)
            {
                // Check if we should use last active browser
                var settings = SettingsManager.LoadSettings();
                Logger.Log($"UseLastActiveBrowser: {settings.UseLastActiveBrowser}, HasRecent: {SettingsManager.HasRecentLastActiveBrowser()}");

                if (settings.UseLastActiveBrowser && SettingsManager.HasRecentLastActiveBrowser())
                {
                    // Try to find and select the last active browser
                    Logger.Log($"Looking for last active browser: {settings.LastActiveBrowserName}");
                    var lastBrowserIndex = browsers.FindIndex(b =>
                        b.Name == settings.LastActiveBrowserName ||
                        b.ExecutablePath == settings.LastActiveBrowserPath);

                    BrowserComboBox.SelectedIndex = lastBrowserIndex >= 0 ? lastBrowserIndex : 0;
                    Logger.Log($"Selected browser index: {BrowserComboBox.SelectedIndex}");
                }
                else
                {
                    BrowserComboBox.SelectedIndex = 0;
                    Logger.Log("Using first browser (no recent last active)");
                }
            }
            else
            {
                Logger.Log("No browsers found!");
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
                    var settings = SettingsManager.LoadSettings();

                    // If this is the last active browser, try to select the last active profile
                    if (settings.UseLastActiveBrowser &&
                        SettingsManager.HasRecentLastActiveBrowser() &&
                        (browser.Name == settings.LastActiveBrowserName ||
                         browser.ExecutablePath == settings.LastActiveBrowserPath))
                    {
                        var lastProfileIndex = profiles.FindIndex(p =>
                            p.Name == settings.LastActiveProfileName);

                        ProfileComboBox.SelectedIndex = lastProfileIndex >= 0 ? lastProfileIndex : 0;
                    }
                    else
                    {
                        ProfileComboBox.SelectedIndex = 0;
                    }
                }
            }
        }

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Reserved for future use
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("OpenButton_Click - user clicked Open button");
            if (BrowserComboBox.SelectedItem is BrowserInfo browser &&
                ProfileComboBox.SelectedItem is ProfileInfo profile)
            {
                Logger.Log($"Selected: Browser={browser.Name}, Profile={profile.Name}");

                // Save rule if checkbox is checked
                if (OpenUrlCheckBox.IsChecked == true)
                {
                    Logger.Log("'Remember this choice' checkbox is checked - saving rule");
                    SaveUrlRule(browser, profile);
                }

                Logger.Log("Opening browser from user selection...");
                OpenBrowser(browser, profile);
                Logger.Log("Calling Application.Shutdown()");
                Application.Current.Shutdown();
            }
            else
            {
                Logger.Log("Open clicked but no browser/profile selected - showing warning");
                MessageBox.Show("Please select a browser and profile.",
                    "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OpenBrowser(BrowserInfo? browser, ProfileInfo? profile, bool isDefault = false)
        {
            Logger.Log($"OpenBrowser called - browser: {browser?.Name}, profile: {profile?.Name}, isDefault: {isDefault}");

            if (browser == null)
            {
                Logger.Log("OpenBrowser failed: browser is null - aborting");
                return;
            }

            try
            {
                Logger.Log($"Creating ProcessStartInfo for: {browser.ExecutablePath}");
                var startInfo = new ProcessStartInfo
                {
                    FileName = browser.ExecutablePath,
                    UseShellExecute = true
                };

                if (!isDefault && profile != null && !string.IsNullOrEmpty(profile.Arguments))
                {
                    Logger.Log($"Using profile arguments: {profile.Arguments}");
                    startInfo.Arguments = $"{profile.Arguments} \"{_url}\"";
                    Logger.Log($"Full command: {browser.ExecutablePath} {startInfo.Arguments}");
                }
                else
                {
                    Logger.Log("No profile arguments - opening with URL only");
                    startInfo.Arguments = $"\"{_url}\"";
                    Logger.Log($"Full command: {browser.ExecutablePath} \"{_url}\"");
                }

                Logger.Log("Starting browser process...");
                Process.Start(startInfo);
                Logger.Log("Browser process started successfully");

                // Save last active browser/profile for future auto-selection
                Logger.Log("Updating last active browser settings...");
                SettingsManager.UpdateLastActiveBrowser(browser, profile ?? new ProfileInfo { Name = "", Arguments = "" });
                Logger.Log("Last active browser updated");
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
            Logger.Log("SettingsButton_Click - opening settings window");
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
            Logger.Log("Settings window closed");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("CancelButton_Click - user cancelled - shutting down");
            Application.Current.Shutdown();
        }
    }
}
