using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BrowserSelector.Services;
using Microsoft.Win32;

namespace BrowserSelector.Pages
{
    public partial class SettingsPage : UserControl
    {
        public event EventHandler? DataImported;

        public SettingsPage()
        {
            InitializeComponent();
            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        public void LoadData()
        {
            InitializeRulesToggle();
            InitializeClipboardMonitoringSettings();
            InitializeNotificationsToggle();
            UpdateRegistrationStatus();
        }

        private void UpdateRegistrationStatus()
        {
            try
            {
                bool isDefaultBrowser = RegistryHelper.IsSystemDefaultBrowser();

                if (isDefaultBrowser)
                {
                    // Green badge - Set
                    RegistrationStatusBorder.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(223, 246, 221)); // #DFF6DD
                    RegistrationStatusBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(165, 214, 167)); // #A5D6A7
                    RegistrationStatusBorder.BorderThickness = new Thickness(1);
                    StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(16, 124, 16)); // #107C10
                    StatusText.Text = "Set";
                    StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(16, 124, 16));
                }
                else
                {
                    // Orange badge - Not Set
                    RegistrationStatusBorder.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(255, 243, 224)); // #FFF3E0
                    RegistrationStatusBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(255, 224, 178)); // #FFE0B2
                    RegistrationStatusBorder.BorderThickness = new Thickness(1);
                    StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(202, 133, 0)); // #CA8500
                    StatusText.Text = "Not Set";
                    StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(202, 133, 0));
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"UpdateRegistrationStatus ERROR: {ex.Message}");
            }
        }

        private void InitializeRulesToggle()
        {
            try
            {
                var settings = SettingsManager.LoadSettings();

                // Set toggle state without triggering the event
                RulesEnabledToggle.Checked -= RulesEnabledToggle_Changed;
                RulesEnabledToggle.Unchecked -= RulesEnabledToggle_Changed;
                RulesEnabledToggle.IsChecked = settings.IsEnabled;
                RulesEnabledToggle.Checked += RulesEnabledToggle_Changed;
                RulesEnabledToggle.Unchecked += RulesEnabledToggle_Changed;
            }
            catch (Exception ex)
            {
                Logger.Log($"InitializeRulesToggle ERROR: {ex.Message}");
            }
        }

        private void RulesEnabledToggle_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                var isEnabled = RulesEnabledToggle.IsChecked == true;

                // Save setting
                var settings = SettingsManager.LoadSettings();
                settings.IsEnabled = isEnabled;
                SettingsManager.SaveSettings(settings);

                Logger.Log($"Rules processing {(isEnabled ? "enabled" : "disabled")}");

                // Update navigation indicator in parent window
                if (Window.GetWindow(this) is SettingsWindow settingsWindow)
                {
                    settingsWindow.UpdateNavigationStatus();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"RulesEnabledToggle_Changed ERROR: {ex.Message}");
            }
        }

        private void InitializeNotificationsToggle()
        {
            try
            {
                var settings = SettingsManager.LoadSettings();

                // Set toggle state without triggering the event
                NotificationsToggle.Checked -= NotificationsToggle_Changed;
                NotificationsToggle.Unchecked -= NotificationsToggle_Changed;
                NotificationsToggle.IsChecked = settings.ShowNotifications;
                NotificationsToggle.Checked += NotificationsToggle_Changed;
                NotificationsToggle.Unchecked += NotificationsToggle_Changed;
            }
            catch (Exception ex)
            {
                Logger.Log($"InitializeNotificationsToggle ERROR: {ex.Message}");
            }
        }

        private void NotificationsToggle_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                var isEnabled = NotificationsToggle.IsChecked == true;

                var settings = SettingsManager.LoadSettings();
                settings.ShowNotifications = isEnabled;
                SettingsManager.SaveSettings(settings);

                Logger.Log($"Unmatched URL notifications {(isEnabled ? "enabled" : "disabled")}");
            }
            catch (Exception ex)
            {
                Logger.Log($"NotificationsToggle_Changed ERROR: {ex.Message}");
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

        private void ExportData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Export LinkRouter Data",
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = $"LinkRouter_Backup_{DateTime.Now:yyyy-MM-dd}"
                };

                if (dialog.ShowDialog() == true)
                {
                    DataExportService.Export(dialog.FileName);

                    MessageBox.Show(
                        $"Data exported successfully!\n\nFile: {dialog.FileName}\n\n" +
                        "This backup includes:\n" +
                        "- All URL rules\n" +
                        "- URL groups and overrides\n" +
                        "- Application settings",
                        "Export Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"ExportData_Click ERROR: {ex.Message}");
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Import LinkRouter Data",
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "json"
                };

                if (dialog.ShowDialog() == true)
                {
                    // Validate the file first
                    var (valid, message, package) = DataExportService.ValidateExportFile(dialog.FileName);

                    if (!valid)
                    {
                        MessageBox.Show($"Cannot import this file:\n\n{message}", "Invalid File",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Confirm import
                    var confirmResult = MessageBox.Show(
                        $"{message}\n\n" +
                        "This will replace your current data.\n" +
                        "A backup will be created automatically before importing.\n\n" +
                        "Do you want to continue?",
                        "Confirm Import",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (confirmResult != MessageBoxResult.Yes)
                        return;

                    // Perform import
                    var result = DataExportService.Import(dialog.FileName, replaceExisting: true);

                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Import Complete",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Refresh the page
                        LoadData();

                        // Notify parent to refresh navigation counts
                        DataImported?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        MessageBox.Show($"Import failed:\n\n{result.Message}", "Import Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"ImportData_Click ERROR: {ex.Message}");
                MessageBox.Show($"Import failed: {ex.Message}", "Import Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Clipboard Monitoring

        private void InitializeClipboardMonitoringSettings()
        {
            try
            {
                var settings = SettingsManager.LoadSettings().ClipboardMonitoring;
                var serviceRunning = ClipboardMonitorService.Instance.IsRunning;

                // Determine actual state from settings
                bool shouldBeEnabled = settings.IsEnabled;

                // Set toggle state without triggering events
                ClipboardMonitoringToggle.Checked -= ClipboardMonitoringToggle_Changed;
                ClipboardMonitoringToggle.Unchecked -= ClipboardMonitoringToggle_Changed;
                ClipboardMonitoringToggle.IsChecked = shouldBeEnabled;
                ClipboardMonitoringToggle.Checked += ClipboardMonitoringToggle_Changed;
                ClipboardMonitoringToggle.Unchecked += ClipboardMonitoringToggle_Changed;

                // CRITICAL: If settings say enabled but service isn't running, start it now
                // This fixes desync after app crash/restart
                if (shouldBeEnabled && !serviceRunning)
                {
                    Logger.Log("InitializeClipboardMonitoringSettings: Service not running but should be - starting now");
                    ClipboardMonitorService.Instance.Start();
                    TrayIconService.Instance.Show();
                    TrayIconService.Instance.UpdateState();
                }

                Logger.Log($"InitializeClipboardMonitoringSettings: Toggle={shouldBeEnabled}, ServiceRunning={serviceRunning}");
            }
            catch (Exception ex)
            {
                Logger.Log($"InitializeClipboardMonitoringSettings ERROR: {ex.Message}");
            }
        }

        private void ClipboardMonitoringToggle_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                var isEnabled = ClipboardMonitoringToggle.IsChecked == true;

                var settings = SettingsManager.LoadSettings();
                settings.ClipboardMonitoring.IsEnabled = isEnabled;
                SettingsManager.SaveSettings(settings);

                // Start or stop the clipboard monitor service
                if (isEnabled)
                {
                    ClipboardMonitorService.Instance.Start();
                    TrayIconService.Instance.Show();
                    TrayIconService.Instance.UpdateState();

                    // Register in Windows startup apps
                    RegistryHelper.AddToStartup();
                }
                else
                {
                    ClipboardMonitorService.Instance.Stop();
                    TrayIconService.Instance.Hide();

                    // Remove from Windows startup apps
                    RegistryHelper.RemoveFromStartup();
                }

                Logger.Log($"Clipboard monitoring {(isEnabled ? "enabled" : "disabled")}");
            }
            catch (Exception ex)
            {
                Logger.Log($"ClipboardMonitoringToggle_Changed ERROR: {ex.Message}");
            }
        }

        #endregion

        /* Profile Auto-Detection region disabled
        #region Profile Auto-Detection

        private void AutoDetectToggle_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                var isEnabled = AutoDetectToggle.IsChecked == true;

                // Save setting
                var settings = SettingsManager.LoadSettings();
                settings.AutoDetectM365Profile = isEnabled;
                SettingsManager.SaveSettings(settings);

                if (isEnabled)
                {
                    // Show overwrite option
                    OverwriteOptionCard.Visibility = Visibility.Visible;

                    // Perform detection and update status
                    UpdateAutoDetectStatus();
                }
                else
                {
                    // Hide options and status
                    OverwriteOptionCard.Visibility = Visibility.Collapsed;
                    AutoDetectStatusCard.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"AutoDetectToggle_Changed ERROR: {ex.Message}");
            }
        }

        private void OverwriteCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                var isOverwrite = OverwriteCheckbox.IsChecked == true;

                // Save setting
                var settings = SettingsManager.LoadSettings();
                settings.AutoDetectOverwriteExisting = isOverwrite;
                SettingsManager.SaveSettings(settings);

                // Re-run detection with new overwrite setting
                if (AutoDetectToggle.IsChecked == true)
                {
                    UpdateAutoDetectStatus();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"OverwriteCheckbox_Changed ERROR: {ex.Message}");
            }
        }

        private void UpdateAutoDetectStatus()
        {
            try
            {
                // Show status card
                AutoDetectStatusCard.Visibility = Visibility.Visible;

                // Detect Windows account
                var account = WindowsAccountService.GetPrimaryAccount();

                if (account == null)
                {
                    // No account detected - show warning
                    SetAutoDetectStatusWarning(
                        "No Windows Account Detected",
                        "Sign in to Azure AD or Office 365 to use this feature");
                    MatchingProfilesPanel.Visibility = Visibility.Collapsed;
                    AppliedStatusText.Visibility = Visibility.Collapsed;
                    return;
                }

                // Find matching profiles
                var matchResults = WindowsAccountService.FindMatchingProfiles(account);
                var matchingProfiles = matchResults.Where(r => r.IsMatch).ToList();

                if (matchingProfiles.Count == 0)
                {
                    // No matching profiles - show warning
                    SetAutoDetectStatusWarning(
                        "No Matching Profiles Found",
                        $"Account: {account.UserEmail}\nSign into Edge/Chrome with your work account");
                    MatchingProfilesPanel.Visibility = Visibility.Collapsed;
                    AppliedStatusText.Visibility = Visibility.Collapsed;
                    return;
                }

                // Check if M365 group already has profiles
                var m365Group = UrlGroupManager.LoadGroups()
                    .FirstOrDefault(g => g.Id == "builtin-m365");
                var groupHasProfiles = m365Group?.Profiles?.Count > 0 ||
                                       !string.IsNullOrEmpty(m365Group?.DefaultBrowserPath);

                var settings = SettingsManager.LoadSettings();
                var shouldApply = !groupHasProfiles || settings.AutoDetectOverwriteExisting;

                // Update status display
                if (shouldApply)
                {
                    // Apply profiles and show success
                    ApplyProfilesToM365Group(matchingProfiles);
                    SetAutoDetectStatusSuccess(
                        "Account Detected",
                        $"{account.UserEmail} ({account.SourceDescription})");
                    AppliedStatusText.Text = "✓ Applied to Microsoft 365 URL Group";
                    AppliedStatusText.Foreground = new SolidColorBrush(Color.FromRgb(16, 124, 16));
                }
                else
                {
                    // Group already configured, show info
                    SetAutoDetectStatusInfo(
                        "Account Detected",
                        $"{account.UserEmail} ({account.SourceDescription})");
                    AppliedStatusText.Text = "ℹ M365 group already has profiles. Enable \"Overwrite\" to apply.";
                    AppliedStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 212));
                }

                // Show matching profiles list
                MatchingProfilesPanel.Visibility = Visibility.Visible;
                AppliedStatusText.Visibility = Visibility.Visible;

                var profileDisplayList = matchingProfiles.Select(p =>
                    $"{p.Browser.Name} - {p.Profile.Name} ({p.ConfidenceDescription})").ToList();
                MatchingProfilesList.ItemsSource = profileDisplayList;

                // Save detected info to settings
                settings.DetectedAccountEmail = account.UserEmail;
                settings.DetectedProfileCount = matchingProfiles.Count;
                SettingsManager.SaveSettings(settings);
            }
            catch (Exception ex)
            {
                Logger.Log($"UpdateAutoDetectStatus ERROR: {ex.Message}");
                SetAutoDetectStatusWarning("Detection Error", ex.Message);
            }
        }

        private void SetAutoDetectStatusSuccess(string title, string detail)
        {
            AutoDetectStatusText.Text = title;
            AutoDetectStatusText.Foreground = new SolidColorBrush(Color.FromRgb(16, 124, 16));
            AutoDetectDetailText.Text = detail;
            AutoDetectStatusIconBorder.Background = new SolidColorBrush(Color.FromRgb(223, 246, 221));
            AutoDetectStatusIcon.Foreground = new SolidColorBrush(Color.FromRgb(16, 124, 16));
            AutoDetectStatusIcon.Text = "\uE73E"; // Check icon
            AutoDetectStatusCard.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
            AutoDetectStatusCard.BorderBrush = new SolidColorBrush(Color.FromRgb(200, 230, 201));
            AutoDetectStatusCard.BorderThickness = new Thickness(1);
        }

        private void SetAutoDetectStatusWarning(string title, string detail)
        {
            AutoDetectStatusText.Text = title;
            AutoDetectStatusText.Foreground = new SolidColorBrush(Color.FromRgb(202, 133, 0));
            AutoDetectDetailText.Text = detail;
            AutoDetectStatusIconBorder.Background = new SolidColorBrush(Color.FromRgb(255, 244, 206));
            AutoDetectStatusIcon.Foreground = new SolidColorBrush(Color.FromRgb(202, 133, 0));
            AutoDetectStatusIcon.Text = "\uE7BA"; // Warning icon
            AutoDetectStatusCard.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
            AutoDetectStatusCard.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 224, 178));
            AutoDetectStatusCard.BorderThickness = new Thickness(1);
        }

        private void SetAutoDetectStatusInfo(string title, string detail)
        {
            AutoDetectStatusText.Text = title;
            AutoDetectStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 212));
            AutoDetectDetailText.Text = detail;
            AutoDetectStatusIconBorder.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
            AutoDetectStatusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 212));
            AutoDetectStatusIcon.Text = "\uE946"; // Info icon
            AutoDetectStatusCard.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
            AutoDetectStatusCard.BorderBrush = new SolidColorBrush(Color.FromRgb(144, 202, 249));
            AutoDetectStatusCard.BorderThickness = new Thickness(1);
        }

        private void ApplyProfilesToM365Group(List<ProfileMatchResult> matchingProfiles)
        {
            try
            {
                var m365Group = UrlGroupManager.LoadGroups()
                    .FirstOrDefault(g => g.Id == "builtin-m365");

                if (m365Group == null)
                {
                    Logger.Log("ApplyProfilesToM365Group: M365 group not found");
                    return;
                }

                // Convert to RuleProfile list
                var profiles = new List<RuleProfile>();
                int order = 0;

                foreach (var match in matchingProfiles)
                {
                    profiles.Add(new RuleProfile
                    {
                        BrowserName = match.Browser.Name,
                        BrowserPath = match.Browser.ExecutablePath,
                        BrowserType = match.Browser.Type,
                        ProfileName = match.Profile.Name,
                        ProfilePath = match.Profile.Path,
                        ProfileArguments = match.Profile.Arguments,
                        DisplayOrder = order++
                    });
                }

                // Update the group
                m365Group.Profiles = profiles;

                // Also set legacy fields from first profile for compatibility
                if (profiles.Count > 0)
                {
                    var first = profiles[0];
                    m365Group.DefaultBrowserName = first.BrowserName;
                    m365Group.DefaultBrowserPath = first.BrowserPath;
                    m365Group.DefaultBrowserType = first.BrowserType;
                    m365Group.DefaultProfileName = first.ProfileName;
                    m365Group.DefaultProfilePath = first.ProfilePath;
                    m365Group.DefaultProfileArguments = first.ProfileArguments;
                }

                // Set behavior based on profile count
                m365Group.Behavior = profiles.Count > 1
                    ? UrlGroupBehavior.ShowProfilePicker
                    : UrlGroupBehavior.UseDefault;

                // Enable the group if not already
                m365Group.IsEnabled = true;

                // Save
                UrlGroupManager.UpdateGroup(m365Group);

                Logger.Log($"ApplyProfilesToM365Group: Applied {profiles.Count} profiles to M365 group");
            }
            catch (Exception ex)
            {
                Logger.Log($"ApplyProfilesToM365Group ERROR: {ex.Message}");
            }
        }

        #endregion
        End of Profile Auto-Detection region */

    }
}
