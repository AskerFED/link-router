using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
                    var profiles = ProfileComboBox.ItemsSource as IEnumerable<ProfileInfo>;
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
                ProfileComboBox.ItemsSource = profiles;

                if (profiles.Count > 0)
                {
                    ProfileComboBox.SelectedIndex = 0;
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (BrowserComboBox.SelectedItem is BrowserInfoWithColor browser &&
                ProfileComboBox.SelectedItem is ProfileInfo profile)
            {
                SelectedBrowser = browser;
                SelectedProfile = profile;
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
