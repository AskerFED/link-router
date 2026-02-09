using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace BrowserSelector
{
    /// <summary>
    /// Represents a URL pattern with metadata for tracking and versioning
    /// </summary>
    public class UrlPattern
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Pattern { get; set; } = string.Empty;
        public bool IsBuiltIn { get; set; } = false;
        public string? VersionAdded { get; set; }  // e.g., "1.0.0", "1.1.0"
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

        /// <summary>
        /// Display-friendly date showing when this pattern was last modified
        /// </summary>
        [JsonIgnore]
        public string DateDisplay => UpdatedDate.HasValue
            ? $"Updated {UpdatedDate.Value:MMM d, yyyy}"
            : $"Added {CreatedDate:MMM d, yyyy}";
    }

    /// <summary>
    /// Helper class for displaying indexed URL patterns in lists
    /// </summary>
    public class IndexedUrlPattern
    {
        public int Index { get; set; }
        public string Pattern { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public UrlPattern? PatternObject { get; set; }
    }

    /// <summary>
    /// Defines the behavior when a URL matches a URL group
    /// </summary>
    public enum UrlGroupBehavior
    {
        /// <summary>
        /// Auto-open with the default browser/profile configured for this group
        /// </summary>
        UseDefault = 0,

        /// <summary>
        /// Show profile picker when URL matches (multiple profiles configured)
        /// </summary>
        ShowProfilePicker = 1
    }

    /// <summary>
    /// Represents a group of URL patterns that share common browser/profile settings
    /// </summary>
    public class UrlGroup
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsBuiltIn { get; set; } = false;
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// URL patterns with metadata (new format)
        /// </summary>
        public List<UrlPattern> UrlPatterns { get; set; } = new List<UrlPattern>();

        /// <summary>
        /// Legacy URL patterns for backward compatibility during migration.
        /// Only used when reading old JSON files.
        /// </summary>
        [JsonPropertyName("UrlPatternsLegacy")]
        public List<string>? LegacyUrlPatterns { get; set; }

        /// <summary>
        /// IDs of built-in patterns the user has deleted.
        /// These won't be re-added on template updates.
        /// </summary>
        public List<string> DeletedBuiltInPatternIds { get; set; } = new List<string>();

        // Default browser/profile for this group (used when Behavior = UseDefault)
        public string DefaultBrowserName { get; set; } = string.Empty;
        public string DefaultBrowserPath { get; set; } = string.Empty;
        public string DefaultBrowserType { get; set; } = string.Empty;
        public string DefaultProfileName { get; set; } = string.Empty;
        public string DefaultProfilePath { get; set; } = string.Empty;
        public string DefaultProfileArguments { get; set; } = string.Empty;

        // Profile group link (used when Behavior = ShowProfilePicker)
        public string LinkedProfileGroupId { get; set; } = string.Empty;

        /// <summary>
        /// Multiple browser/profile options for this group.
        /// When more than one profile is configured, user will be shown a picker.
        /// </summary>
        public List<RuleProfile> Profiles { get; set; } = new List<RuleProfile>();

        // Behavior mode
        public UrlGroupBehavior Behavior { get; set; } = UrlGroupBehavior.UseDefault;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Display-friendly count of URL patterns
        /// </summary>
        public int PatternCount => UrlPatterns?.Count ?? 0;

        /// <summary>
        /// URL patterns with index for numbered list display
        /// </summary>
        [JsonIgnore]
        public List<IndexedUrlPattern> UrlPatternsIndexed =>
            UrlPatterns?.Select((p, i) => new IndexedUrlPattern { Index = i + 1, Pattern = p.Pattern, GroupId = this.Id, PatternObject = p }).ToList()
            ?? new List<IndexedUrlPattern>();

        /// <summary>
        /// Returns true if this group has multiple profiles configured
        /// </summary>
        public bool HasMultipleProfiles => Profiles?.Count > 1;

        /// <summary>
        /// Display-friendly behavior description
        /// </summary>
        public string BehaviorDisplay => HasMultipleProfiles || Behavior == UrlGroupBehavior.ShowProfilePicker
            ? "Show picker"
            : "Auto-open";

        /// <summary>
        /// Display-friendly default setting
        /// </summary>
        public string DefaultDisplay
        {
            get
            {
                if (HasMultipleProfiles)
                    return $"{Profiles.Count} profiles configured";
                if (!string.IsNullOrEmpty(DefaultBrowserName))
                    return $"{DefaultBrowserName} / {DefaultProfileName}";
                if (Profiles?.Count == 1)
                    return $"{Profiles[0].BrowserName} / {Profiles[0].ProfileName}";
                return "Not set";
            }
        }

        /// <summary>
        /// Display-friendly mode (Auto-open or Picker)
        /// </summary>
        public string ModeDisplay => HasMultipleProfiles || Behavior == UrlGroupBehavior.ShowProfilePicker
            ? "Shows Prompt"
            : "Auto-open";

        /// <summary>
        /// Display-friendly profile count
        /// </summary>
        public string ProfileCountDisplay
        {
            get
            {
                var count = Profiles?.Count ?? 0;
                if (count == 0 && !string.IsNullOrEmpty(DefaultBrowserName))
                    count = 1;
                return count == 1 ? "1 profile configured" : $"{count} profiles configured";
            }
        }
    }

    /// <summary>
    /// Represents a URL-specific override within a URL group
    /// Allows specific URLs to have different behavior than the group default
    /// </summary>
    public class UrlGroupOverride
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UrlGroupId { get; set; } = string.Empty;
        public string UrlPattern { get; set; } = string.Empty;

        // Override browser/profile (used when Behavior = UseDefault)
        public string BrowserName { get; set; } = string.Empty;
        public string BrowserPath { get; set; } = string.Empty;
        public string BrowserType { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;
        public string ProfilePath { get; set; } = string.Empty;
        public string ProfileArguments { get; set; } = string.Empty;

        // Or override to use a different profile group
        public string LinkedProfileGroupId { get; set; } = string.Empty;

        public UrlGroupBehavior Behavior { get; set; } = UrlGroupBehavior.UseDefault;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
