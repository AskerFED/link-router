using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BrowserSelector.Models;
using BrowserSelector.Services;

namespace BrowserSelector
{
    public partial class AddProfileDialog : Window
    {
        private List<BrowserInfoWithColor> _browsers;
        private RuleProfile? _editingProfile;
        private bool _isEditMode;

        public BrowserInfoWithColor? SelectedBrowser { get; private set; }
        public ProfileInfo? SelectedProfile { get; private set; }
        public string CustomDisplayName { get; private set; } = string.Empty;

        /// <summary>
        /// Constructor for adding a new profile
        /// </summary>
        public AddProfileDialog()
        {
            InitializeComponent();
            _isEditMode = false;
            _browsers = new List<BrowserInfoWithColor>();
            LoadBrowsers();
        }

        /// <summary>
        /// Constructor for editing an existing profile
        /// </summary>
        public AddProfileDialog(RuleProfile existingProfile) : this()
        {
            _isEditMode = true;
            _editingProfile = existingProfile;

            // Update UI for edit mode
            DialogWindow.Title = "Edit Profile";
            ActionButton.Content = "Save";

            // Pre-populate custom name
            CustomNameTextBox.Text = existingProfile.CustomDisplayName ?? string.Empty;

            // Pre-select browser and profile after loading
            Loaded += (s, e) => PreSelectExistingProfile();
        }

        private void PreSelectExistingProfile()
        {
            if (_editingProfile == null || _browsers == null) return;

            // Find and select the matching browser
            var matchingBrowser = _browsers.FirstOrDefault(b =>
                b.ExecutablePath == _editingProfile.BrowserPath);

            if (matchingBrowser != null)
            {
                BrowserComboBox.SelectedItem = matchingBrowser;

                // Wait for profiles to load, then select matching profile
                Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    var profiles = ProfileComboBox.ItemsSource as IEnumerable<ProfileDisplayInfo>;
                    if (profiles != null)
                    {
                        var matchingProfile = profiles.FirstOrDefault(p =>
                            p.Path == _editingProfile.ProfilePath);
                        if (matchingProfile != null)
                        {
                            ProfileComboBox.SelectedItem = matchingProfile;
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void LoadBrowsers()
        {
            _browsers = BrowserService.GetBrowsersWithColors();
            BrowserComboBox.ItemsSource = _browsers;

            if (_browsers.Count > 0)
            {
                BrowserComboBox.SelectedIndex = 0;
            }
        }

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

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (BrowserComboBox.SelectedItem is BrowserInfoWithColor browser &&
                ProfileComboBox.SelectedItem is ProfileDisplayInfo displayProfile)
            {
                SelectedBrowser = browser;
                // Convert back to ProfileInfo for callers
                SelectedProfile = new ProfileInfo
                {
                    Name = displayProfile.Name,
                    Email = displayProfile.Email,
                    Path = displayProfile.Path,
                    Arguments = displayProfile.Arguments,
                    AvatarUrl = displayProfile.AvatarUrl,
                    ProfileIndex = displayProfile.ProfileIndex
                };
                CustomDisplayName = CustomNameTextBox.Text?.Trim() ?? string.Empty;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a browser and profile.",
                    "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
