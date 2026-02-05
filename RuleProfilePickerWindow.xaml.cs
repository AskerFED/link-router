using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using BrowserSelector.Services;

namespace BrowserSelector
{
    /// <summary>
    /// Window for selecting a profile from a multi-profile rule or URL group
    /// </summary>
    public partial class RuleProfilePickerWindow : Window
    {
        private string _url;
        private UrlRule? _rule;
        private UrlGroup? _group;
        private List<RuleProfileDisplay> _displayProfiles;
        private bool _isGroupMode;

        /// <summary>
        /// Constructor for multi-profile URL rule
        /// </summary>
        public RuleProfilePickerWindow(string url, UrlRule rule)
        {
            InitializeComponent();

            _url = url;
            _rule = rule;
            _isGroupMode = false;

            LoadRuleData();
        }

        /// <summary>
        /// Constructor for multi-profile URL group
        /// </summary>
        public RuleProfilePickerWindow(string url, UrlGroup group)
        {
            InitializeComponent();

            _url = url;
            _group = group;
            _isGroupMode = true;

            LoadGroupData();
        }

        private void LoadRuleData()
        {
            // Display URL
            UrlTextBlock.Text = _url;

            // Display rule pattern
            RulePatternRun.Text = _rule!.Pattern;

            // Load profiles with display info
            _displayProfiles = _rule.Profiles
                .OrderBy(p => p.DisplayOrder)
                .Select(p => new RuleProfileDisplay
                {
                    Id = p.Id,
                    BrowserName = p.BrowserName,
                    BrowserPath = p.BrowserPath,
                    BrowserType = p.BrowserType,
                    ProfileName = p.ProfileName,
                    ProfilePath = p.ProfilePath,
                    ProfileArguments = p.ProfileArguments,
                    DisplayOrder = p.DisplayOrder,
                    CustomDisplayName = p.CustomDisplayName,
                    BrowserColor = BrowserService.BrowserColors.TryGetValue(p.BrowserName, out var color) ? color : "#666666"
                })
                .ToList();

            ProfilesItemsControl.ItemsSource = _displayProfiles;

            if (_displayProfiles.Count == 0)
            {
                MessageBox.Show("No profiles in this rule.", "Empty Rule", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LoadGroupData()
        {
            // Display URL
            UrlTextBlock.Text = _url;

            // Update the label text to say "Matched Group:" instead of "Matched Rule:"
            MatchedRuleTextBlock.Inlines.Clear();
            MatchedRuleTextBlock.Inlines.Add(new Run("Matched Group: ") { FontWeight = FontWeights.SemiBold });
            MatchedRuleTextBlock.Inlines.Add(new Run(_group!.Name));

            // Load profiles with display info
            _displayProfiles = _group.Profiles
                .OrderBy(p => p.DisplayOrder)
                .Select(p => new RuleProfileDisplay
                {
                    Id = p.Id,
                    BrowserName = p.BrowserName,
                    BrowserPath = p.BrowserPath,
                    BrowserType = p.BrowserType,
                    ProfileName = p.ProfileName,
                    ProfilePath = p.ProfilePath,
                    ProfileArguments = p.ProfileArguments,
                    DisplayOrder = p.DisplayOrder,
                    CustomDisplayName = p.CustomDisplayName,
                    BrowserColor = BrowserService.BrowserColors.TryGetValue(p.BrowserName, out var color) ? color : "#666666"
                })
                .ToList();

            ProfilesItemsControl.ItemsSource = _displayProfiles;

            if (_displayProfiles.Count == 0)
            {
                MessageBox.Show("No profiles in this group.", "Empty Group", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Profile_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is RuleProfileDisplay profile)
            {
                // Open browser with selected profile
                OpenBrowser(profile);

                // Close window
                Close();
            }
        }

        private void OpenBrowser(RuleProfileDisplay profile)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = profile.BrowserPath,
                    Arguments = $"{profile.ProfileArguments} \"{_url}\"",
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                Logger.Log($"Opened {profile.BrowserName} with profile {profile.ProfileName}");

                // Save last active browser/profile for future auto-selection
                var browserInfo = new BrowserInfo { Name = profile.BrowserName, ExecutablePath = profile.BrowserPath, Type = profile.BrowserType };
                var profileInfo = new ProfileInfo { Name = profile.ProfileName, Path = profile.ProfilePath, Arguments = profile.ProfileArguments };
                SettingsManager.UpdateLastActiveBrowser(browserInfo, profileInfo);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error opening browser: {ex.Message}");
                MessageBox.Show($"Error opening browser: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    /// <summary>
    /// Extended RuleProfile with display properties
    /// </summary>
    internal class RuleProfileDisplay : RuleProfile
    {
        public string BrowserColor { get; set; } = "#666666";

        /// <summary>
        /// Gets the display name - uses CustomDisplayName if set, otherwise defaults to "BrowserName - ProfileName"
        /// </summary>
        public new string DisplayName => !string.IsNullOrEmpty(CustomDisplayName)
            ? CustomDisplayName
            : (string.IsNullOrEmpty(ProfileName) ? BrowserName : $"{BrowserName} - {ProfileName}");

        /// <summary>
        /// Gets the browser/profile description (always shows "BrowserName - ProfileName")
        /// </summary>
        public new string BrowserProfileDescription => string.IsNullOrEmpty(ProfileName)
            ? BrowserName
            : $"{BrowserName} - {ProfileName}";
    }
}
