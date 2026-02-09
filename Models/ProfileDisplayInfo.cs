using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace BrowserSelector.Models
{
    /// <summary>
    /// Extended ProfileInfo with display properties including avatar for UI binding.
    /// Implements INotifyPropertyChanged for async avatar loading updates.
    /// </summary>
    public class ProfileDisplayInfo : INotifyPropertyChanged
    {
        private ImageSource? _avatar;

        // Core profile properties (from ProfileInfo)
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public int ProfileIndex { get; set; }

        /// <summary>
        /// The loaded avatar image. Null if not yet loaded or unavailable.
        /// </summary>
        public ImageSource? Avatar
        {
            get => _avatar;
            set
            {
                if (_avatar != value)
                {
                    _avatar = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasAvatar));
                }
            }
        }

        /// <summary>
        /// Whether an avatar image is available.
        /// </summary>
        public bool HasAvatar => Avatar != null;

        /// <summary>
        /// First letter of the profile name for fallback display.
        /// </summary>
        public string Initial => string.IsNullOrEmpty(Name) ? "?" : Name[0].ToString().ToUpper();

        /// <summary>
        /// Display name (Name with Email if available).
        /// </summary>
        public string DisplayName =>
            string.IsNullOrWhiteSpace(Email)
                ? Name
                : $"{Name} ({Email})";

        /// <summary>
        /// Extracts the domain portion from the email address.
        /// </summary>
        public string EmailDomain =>
            string.IsNullOrEmpty(Email)
                ? string.Empty
                : Email.Contains("@")
                    ? Email.Substring(Email.IndexOf('@') + 1).ToLowerInvariant()
                    : string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Creates a ProfileDisplayInfo from a ProfileInfo.
        /// </summary>
        public static ProfileDisplayInfo FromProfileInfo(ProfileInfo profile)
        {
            return new ProfileDisplayInfo
            {
                Name = profile.Name,
                Email = profile.Email,
                Path = profile.Path,
                Arguments = profile.Arguments,
                AvatarUrl = profile.AvatarUrl,
                ProfileIndex = profile.ProfileIndex
            };
        }
    }
}
