namespace BrowserSelector.Models
{
    /// <summary>
    /// Confidence level of the profile match.
    /// </summary>
    public enum MatchConfidence
    {
        /// <summary>
        /// Exact email match (user@contoso.com == user@contoso.com).
        /// </summary>
        ExactEmail,

        /// <summary>
        /// Same domain match (*@contoso.com == *@contoso.com).
        /// </summary>
        SameDomain,

        /// <summary>
        /// Subdomain match (*@mail.contoso.com ~ *@contoso.com).
        /// </summary>
        SubdomainMatch,

        /// <summary>
        /// No match - different domain.
        /// </summary>
        NoMatch
    }

    /// <summary>
    /// Represents a match between a Windows account and a browser profile.
    /// </summary>
    public class ProfileMatchResult
    {
        public WindowsAccountInfo Account { get; set; }
        public BrowserInfoWithColor Browser { get; set; }
        public ProfileInfo Profile { get; set; }
        public MatchConfidence Confidence { get; set; }
        public string MatchReason { get; set; } = string.Empty;

        /// <summary>
        /// Whether this match should be selected by default.
        /// Exact and SameDomain matches are selected by default.
        /// </summary>
        public bool IsSelectedByDefault => Confidence == MatchConfidence.ExactEmail ||
                                           Confidence == MatchConfidence.SameDomain;

        /// <summary>
        /// Whether this is a matching profile (not NoMatch).
        /// </summary>
        public bool IsMatch => Confidence != MatchConfidence.NoMatch;

        /// <summary>
        /// Display-friendly confidence description.
        /// </summary>
        public string ConfidenceDescription => Confidence switch
        {
            MatchConfidence.ExactEmail => "Exact email match",
            MatchConfidence.SameDomain => "Same domain match",
            MatchConfidence.SubdomainMatch => "Subdomain match",
            MatchConfidence.NoMatch => "Different domain",
            _ => "Unknown"
        };
    }
}
