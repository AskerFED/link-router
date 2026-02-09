using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BrowserSelector.Models;
using BrowserSelector.Services;

namespace BrowserSelector.Controls
{
    /// <summary>
    /// Reusable UserControl for browser/profile selection with Simple and Advanced modes.
    /// Simple Mode: Single browser/profile selection via dropdowns
    /// Advanced Mode: Multiple profiles with add/edit/remove functionality
    /// </summary>
    public partial class BrowserProfileMultiSelector : UserControl
    {
        private List<BrowserInfoWithColor> _browsers;
        private ObservableCollection<RuleProfile> _profiles = new ObservableCollection<RuleProfile>();
        private bool _isAdvancedMode = false;

        /// <summary>
        /// Event raised when the selection changes
        /// </summary>
        public event EventHandler SelectionChanged;

        /// <summary>
        /// Event raised when the mode changes (Simple <-> Advanced)
        /// </summary>
        public event EventHandler ModeChanged;

        public BrowserProfileMultiSelector()
        {
            InitializeComponent();
            ProfilesList.ItemsSource = _profiles;
            LoadBrowsers();
        }

        #region Public Properties

        /// <summary>
        /// Gets whether the control is currently in Advanced mode
        /// </summary>
        public bool IsAdvancedMode
        {
            get => _isAdvancedMode;
            private set
            {
                if (_isAdvancedMode != value)
                {
                    _isAdvancedMode = value;
                    ModeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Gets whether multiple profiles are currently configured
        /// </summary>
        public bool HasMultipleProfiles => _profiles.Count > 1;

        /// <summary>
        /// Gets the count of configured profiles
        /// </summary>
        public int ProfileCount => IsAdvancedMode ? _profiles.Count : 1;

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads a single profile (for Edit scenarios with single-profile rules/groups)
        /// </summary>
        public void LoadSingleProfile(string browserPath, string profilePath)
        {
            // Find and select the browser
            var browser = _browsers?.FirstOrDefault(b => b.ExecutablePath == browserPath);
            if (browser != null)
            {
                BrowserComboBox.SelectedItem = browser;

                // Wait for profiles to load, then select the matching profile
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var profiles = ProfileComboBox.ItemsSource as IEnumerable<ProfileDisplayInfo>;
                    var matchingProfile = profiles?.FirstOrDefault(p => p.Path == profilePath);
                    if (matchingProfile != null)
                    {
                        ProfileComboBox.SelectedItem = matchingProfile;
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }

            // Stay in Simple mode
            _isAdvancedMode = false;
            SimpleModePanel.Visibility = Visibility.Visible;
            AdvancedModePanel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Loads multiple profiles (for Edit scenarios with multi-profile rules/groups)
        /// </summary>
        public void LoadProfiles(List<RuleProfile> profiles)
        {
            _profiles.Clear();

            if (profiles == null || profiles.Count == 0)
            {
                // No profiles - stay in Simple mode
                _isAdvancedMode = false;
                SimpleModePanel.Visibility = Visibility.Visible;
                AdvancedModePanel.Visibility = Visibility.Collapsed;
                return;
            }

            if (profiles.Count == 1)
            {
                // Single profile - load into Simple mode
                var p = profiles[0];
                LoadSingleProfile(p.BrowserPath, p.ProfilePath);
                return;
            }

            // Multiple profiles - switch to Advanced mode
            foreach (var p in profiles)
            {
                _profiles.Add(p);
            }

            _isAdvancedMode = true;
            SimpleModePanel.Visibility = Visibility.Collapsed;
            AdvancedModePanel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Gets a single profile from the current selection (for Simple mode)
        /// </summary>
        public RuleProfile GetSingleProfile()
        {
            if (IsAdvancedMode && _profiles.Count > 0)
            {
                return _profiles[0];
            }

            if (BrowserComboBox.SelectedItem is BrowserInfoWithColor browser &&
                ProfileComboBox.SelectedItem is ProfileDisplayInfo profile)
            {
                return new RuleProfile
                {
                    BrowserName = browser.Name,
                    BrowserPath = browser.ExecutablePath,
                    BrowserType = browser.Type,
                    ProfileName = profile.Name,
                    ProfilePath = profile.Path,
                    ProfileArguments = profile.Arguments,
                    DisplayOrder = 0
                };
            }

            return null;
        }

        /// <summary>
        /// Gets all configured profiles
        /// </summary>
        public List<RuleProfile> GetAllProfiles()
        {
            if (IsAdvancedMode)
            {
                return _profiles.ToList();
            }

            // Simple mode - return single profile as list
            var profile = GetSingleProfile();
            return profile != null ? new List<RuleProfile> { profile } : new List<RuleProfile>();
        }

        /// <summary>
        /// Validates that at least one profile is configured
        /// </summary>
        public bool HasValidSelection()
        {
            if (IsAdvancedMode)
            {
                return _profiles.Count > 0;
            }

            return BrowserComboBox.SelectedItem != null && ProfileComboBox.SelectedItem != null;
        }

        /// <summary>
        /// Refreshes the browser list
        /// </summary>
        public void RefreshBrowsers()
        {
            LoadBrowsers();
        }

        #endregion

        #region Private Methods

        private void LoadBrowsers()
        {
            _browsers = BrowserService.GetBrowsersWithColors();
            BrowserComboBox.ItemsSource = _browsers;

            if (_browsers.Count > 0)
            {
                BrowserComboBox.SelectedIndex = 0;
            }
        }

        private void SwitchToAdvancedMode()
        {
            // Add current selection to profiles list if valid
            if (BrowserComboBox.SelectedItem is BrowserInfoWithColor browser &&
                ProfileComboBox.SelectedItem is ProfileDisplayInfo profile)
            {
                var ruleProfile = new RuleProfile
                {
                    BrowserName = browser.Name,
                    BrowserPath = browser.ExecutablePath,
                    BrowserType = browser.Type,
                    ProfileName = profile.Name,
                    ProfilePath = profile.Path,
                    ProfileArguments = profile.Arguments,
                    DisplayOrder = _profiles.Count
                };

                _profiles.Add(ruleProfile);
            }

            IsAdvancedMode = true;
            SimpleModePanel.Visibility = Visibility.Collapsed;
            AdvancedModePanel.Visibility = Visibility.Visible;
        }

        private void SwitchToSimpleMode()
        {
            // Get the first profile to restore to dropdowns
            var firstProfile = _profiles.FirstOrDefault();

            // Clear profiles and switch mode
            _profiles.Clear();
            IsAdvancedMode = false;
            SimpleModePanel.Visibility = Visibility.Visible;
            AdvancedModePanel.Visibility = Visibility.Collapsed;

            // Pre-select the first profile in the dropdowns
            if (firstProfile != null)
            {
                var matchingBrowser = _browsers?.FirstOrDefault(b => b.ExecutablePath == firstProfile.BrowserPath);
                if (matchingBrowser != null)
                {
                    BrowserComboBox.SelectedItem = matchingBrowser;

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var profiles = ProfileComboBox.ItemsSource as IEnumerable<ProfileDisplayInfo>;
                        var matchingProfile = profiles?.FirstOrDefault(p => p.Path == firstProfile.ProfilePath);
                        if (matchingProfile != null)
                        {
                            ProfileComboBox.SelectedItem = matchingProfile;
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        private void RefreshProfilesList()
        {
            // Force UI refresh
            var profiles = _profiles.ToList();
            _profiles.Clear();
            foreach (var p in profiles)
            {
                _profiles.Add(p);
            }
        }

        private Window GetOwnerWindow()
        {
            return Window.GetWindow(this);
        }

        #endregion

        #region Event Handlers

        private void BrowserComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BrowserComboBox.SelectedItem is BrowserInfoWithColor browser)
            {
                var profiles = BrowserService.GetProfiles(browser);

                // Convert to display models with avatar support
                var displayProfiles = profiles.Select(ProfileDisplayInfo.FromProfileInfo).ToList();
                ProfileComboBox.ItemsSource = displayProfiles;

                if (displayProfiles.Count > 0)
                {
                    ProfileComboBox.SelectedIndex = 0;
                }

                // Load avatars asynchronously in background
                _ = LoadAvatarsAsync(displayProfiles);
            }

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task LoadAvatarsAsync(List<ProfileDisplayInfo> profiles)
        {
            foreach (var profile in profiles)
            {
                if (!string.IsNullOrEmpty(profile.AvatarUrl))
                {
                    var avatar = await ProfileAvatarService.GetAvatarAsync(profile.AvatarUrl, profile.Path);
                    if (avatar != null)
                    {
                        // Update on UI thread - PropertyChanged will refresh the binding
                        await Dispatcher.InvokeAsync(() => profile.Avatar = avatar);
                    }
                }
            }
        }

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void AdvancedPick_Click(object sender, RoutedEventArgs e)
        {
            if (BrowserComboBox.SelectedItem == null || ProfileComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a browser and profile first.",
                    "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SwitchToAdvancedMode();
        }

        private void SwitchToSimple_Click(object sender, RoutedEventArgs e)
        {
            SwitchToSimpleMode();
        }

        private void AddProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddProfileDialog();
            dialog.Owner = GetOwnerWindow();

            if (dialog.ShowDialog() == true)
            {
                var browser = dialog.SelectedBrowser;
                var profile = dialog.SelectedProfile;

                // Check for duplicates
                var exists = _profiles.Any(p =>
                    p.BrowserPath == browser.ExecutablePath &&
                    p.ProfilePath == profile.Path);

                if (exists)
                {
                    MessageBox.Show("This browser/profile combination has already been added.",
                        "Duplicate Profile", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var ruleProfile = new RuleProfile
                {
                    BrowserName = browser.Name,
                    BrowserPath = browser.ExecutablePath,
                    BrowserType = browser.Type,
                    ProfileName = profile.Name,
                    ProfilePath = profile.Path,
                    ProfileArguments = profile.Arguments,
                    DisplayOrder = _profiles.Count,
                    CustomDisplayName = dialog.CustomDisplayName
                };

                _profiles.Add(ruleProfile);
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string profileId)
            {
                var existingProfile = _profiles.FirstOrDefault(p => p.Id == profileId);
                if (existingProfile == null) return;

                var dialog = new AddProfileDialog(existingProfile);
                dialog.Owner = GetOwnerWindow();

                if (dialog.ShowDialog() == true)
                {
                    var browser = dialog.SelectedBrowser;
                    var profile = dialog.SelectedProfile;

                    // Check for duplicates (excluding current profile)
                    var exists = _profiles.Any(p =>
                        p.Id != profileId &&
                        p.BrowserPath == browser.ExecutablePath &&
                        p.ProfilePath == profile.Path);

                    if (exists)
                    {
                        MessageBox.Show("This browser/profile combination already exists in the list.",
                            "Duplicate Profile", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Update the profile
                    existingProfile.BrowserName = browser.Name;
                    existingProfile.BrowserPath = browser.ExecutablePath;
                    existingProfile.BrowserType = browser.Type;
                    existingProfile.ProfileName = profile.Name;
                    existingProfile.ProfilePath = profile.Path;
                    existingProfile.ProfileArguments = profile.Arguments;
                    existingProfile.CustomDisplayName = dialog.CustomDisplayName;

                    RefreshProfilesList();
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void RemoveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string profileId)
            {
                var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
                if (profile != null)
                {
                    _profiles.Remove(profile);

                    // If no profiles left, switch back to Simple mode
                    if (_profiles.Count == 0)
                    {
                        SwitchToSimpleMode();
                    }
                    else
                    {
                        // Update display orders
                        for (int i = 0; i < _profiles.Count; i++)
                        {
                            _profiles[i].DisplayOrder = i;
                        }
                    }

                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        #endregion
    }
}
