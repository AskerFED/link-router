namespace BrowserSelector.Models
{
    /// <summary>
    /// Source of the Windows account detection.
    /// </summary>
    public enum AccountSource
    {
        /// <summary>
        /// Detected from Azure AD Domain Join registry keys.
        /// </summary>
        AzureAdJoin,

        /// <summary>
        /// Detected from Workplace Join (non-domain-joined devices).
        /// </summary>
        WorkplaceJoin,

        /// <summary>
        /// Detected from Office 365 identity cache.
        /// </summary>
        OfficeIdentity
    }

    /// <summary>
    /// Represents a Windows account detected from Azure AD or Office 365.
    /// </summary>
    public class WindowsAccountInfo
    {
        public string TenantId { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public AccountSource Source { get; set; }
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Extracts the domain portion from the email address.
        /// </summary>
        public string Domain
        {
            get
            {
                if (string.IsNullOrEmpty(UserEmail))
                    return string.Empty;

                var atIndex = UserEmail.IndexOf('@');
                return atIndex > 0
                    ? UserEmail.Substring(atIndex + 1).ToLowerInvariant()
                    : string.Empty;
            }
        }

        /// <summary>
        /// Gets a display-friendly description of the account source.
        /// </summary>
        public string SourceDescription => Source switch
        {
            AccountSource.AzureAdJoin => "Azure AD Join",
            AccountSource.WorkplaceJoin => "Workplace Join",
            AccountSource.OfficeIdentity => "Office 365",
            _ => "Unknown"
        };
    }
}
