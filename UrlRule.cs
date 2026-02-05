using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BrowserSelector
{
    /// <summary>
    /// Represents a browser/profile combination within a rule
    /// </summary>
    public class RuleProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string BrowserName { get; set; } = string.Empty;
        public string BrowserPath { get; set; } = string.Empty;
        public string BrowserType { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;
        public string ProfilePath { get; set; } = string.Empty;
        public string ProfileArguments { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Custom display name for this profile (scoped to this rule only).
        /// If set, this name is shown in the picker instead of the default browser/profile name.
        /// </summary>
        public string CustomDisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets the display name - uses CustomDisplayName if set, otherwise defaults to "BrowserName - ProfileName"
        /// </summary>
        public string DisplayName => !string.IsNullOrEmpty(CustomDisplayName)
            ? CustomDisplayName
            : (string.IsNullOrEmpty(ProfileName) ? BrowserName : $"{BrowserName} - {ProfileName}");

        /// <summary>
        /// Gets the default browser/profile description (always shows "BrowserName - ProfileName")
        /// </summary>
        public string BrowserProfileDescription => string.IsNullOrEmpty(ProfileName)
            ? BrowserName
            : $"{BrowserName} - {ProfileName}";
    }

    public class UrlRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Pattern { get; set; } = string.Empty;

        /// <summary>
        /// Whether this rule is enabled and should be used for URL matching.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// List of browser/profile combinations for this rule.
        /// If count is 1, auto-opens. If count > 1, shows picker.
        /// </summary>
        public List<RuleProfile> Profiles { get; set; } = new List<RuleProfile>();

        /// <summary>
        /// Returns true if this rule has multiple profiles (should show picker)
        /// </summary>
        public bool HasMultipleProfiles => Profiles?.Count > 1;

        // Legacy properties for backward compatibility during migration
        // These are used when loading old rules that don't have Profiles list
        public string BrowserName { get; set; } = string.Empty;
        public string BrowserPath { get; set; } = string.Empty;
        public string BrowserType { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;
        public string ProfilePath { get; set; } = string.Empty;
        public string ProfileArguments { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets the first profile for display purposes
        /// </summary>
        public RuleProfile? FirstProfile => Profiles?.FirstOrDefault();

        /// <summary>
        /// Gets a display string showing profile count or single profile name
        /// </summary>
        public string ProfilesDisplay
        {
            get
            {
                if (Profiles == null || Profiles.Count == 0)
                    return ProfileName; // Legacy fallback
                if (Profiles.Count == 1)
                    return Profiles[0].ProfileName;
                return $"{Profiles.Count} profiles";
            }
        }

        /// <summary>
        /// Gets the browser name for display (from first profile or legacy)
        /// </summary>
        public string DisplayBrowserName
        {
            get
            {
                if (Profiles != null && Profiles.Count > 0)
                    return Profiles[0].BrowserName;
                return BrowserName;
            }
        }
    }

    public static class UrlRuleManager
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BrowserSelector",
            "rules.json"
        );

        private static List<UrlRule> _cachedRules = null;

        public static List<UrlRule> LoadRules()
        {
            if (_cachedRules != null)
                return _cachedRules;

            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    _cachedRules = JsonSerializer.Deserialize<List<UrlRule>>(json) ?? new List<UrlRule>();

                    // Migrate old single-profile rules to new Profiles list format
                    bool needsSave = false;
                    foreach (var rule in _cachedRules)
                    {
                        if ((rule.Profiles == null || rule.Profiles.Count == 0) &&
                            !string.IsNullOrEmpty(rule.BrowserName))
                        {
                            rule.Profiles = new List<RuleProfile>
                            {
                                new RuleProfile
                                {
                                    BrowserName = rule.BrowserName,
                                    BrowserPath = rule.BrowserPath,
                                    BrowserType = rule.BrowserType,
                                    ProfileName = rule.ProfileName,
                                    ProfilePath = rule.ProfilePath,
                                    ProfileArguments = rule.ProfileArguments,
                                    DisplayOrder = 0
                                }
                            };
                            needsSave = true;
                        }
                    }

                    // Save migrated rules
                    if (needsSave)
                    {
                        SaveRules(_cachedRules);
                    }

                    return _cachedRules;
                }
            }
            catch
            {
                // If there's an error reading, start fresh
            }

            _cachedRules = new List<UrlRule>();
            return _cachedRules;
        }

        public static void SaveRules(List<UrlRule> rules)
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(rules, options);
                File.WriteAllText(ConfigPath, json);

                _cachedRules = rules;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save rules: {ex.Message}", ex);
            }
        }

        public static void AddRule(UrlRule rule)
        {
            var rules = LoadRules();
            rules.Add(rule);
            SaveRules(rules);
        }

        public static void UpdateRule(UrlRule rule)
        {
            var rules = LoadRules();
            var index = rules.FindIndex(r => r.Id == rule.Id);
            if (index >= 0)
            {
                rules[index] = rule; // Replace at same position to preserve order
                SaveRules(rules);
            }
        }

        public static void DeleteRule(string ruleId)
        {
            var rules = LoadRules();
            rules.RemoveAll(r => r.Id == ruleId);
            SaveRules(rules);
        }

        public static UrlRule FindMatchingRule(string url)
        {
            // Only consider enabled rules for matching
            var rules = LoadRules().Where(r => r.IsEnabled).ToList();

            // Try exact match first
            var exactMatch = rules.FirstOrDefault(r =>
                url.IndexOf(r.Pattern, StringComparison.OrdinalIgnoreCase) >= 0);

            if (exactMatch != null)
                return exactMatch;

            // Try domain match
            try
            {
                var uri = new Uri(url);
                var domain = uri.Host.ToLower();

                return rules.FirstOrDefault(r =>
                    domain.Contains(r.Pattern.ToLower()) ||
                    r.Pattern.ToLower().Contains(domain));
            }
            catch
            {
                return null;
            }
        }

        public static void ClearCache()
        {
            _cachedRules = null;
        }

        /// <summary>
        /// Enhanced URL matching with priority:
        /// 1. URL Groups (auto-open) - highest priority
        /// 2. Profile Groups (shows picker)
        /// 3. Individual Rules (auto-open)
        /// 4. No Match (use default browser or show picker)
        /// </summary>
        public static MatchResult FindMatch(string url)
        {
            // Priority 1: Check for URL Group match with auto-open behavior
            var (group, groupOverride) = UrlGroupManager.FindMatchingGroup(url);

            if (group != null)
            {
                // URL Group with override
                if (groupOverride != null)
                {
                    Logger.Log($"URL matched group override: {group.Name} -> {groupOverride.UrlPattern}");
                    return new MatchResult
                    {
                        Type = MatchType.GroupOverride,
                        Group = group,
                        Override = groupOverride
                    };
                }

                // URL Group (auto-open with default browser/profile)
                Logger.Log($"URL matched URL group: {group.Name}");
                return new MatchResult
                {
                    Type = MatchType.UrlGroup,
                    Group = group
                };
            }

            // Priority 2: Check for individual URL rule match
            var rule = FindMatchingRule(url);
            if (rule != null)
            {
                Logger.Log($"URL matched individual rule: {rule.Pattern}");
                return new MatchResult
                {
                    Type = MatchType.IndividualRule,
                    Rule = rule
                };
            }

            // Priority 4: No match found
            Logger.Log($"No match found for URL: {url}");
            return new MatchResult { Type = MatchType.NoMatch };
        }
    }

    /// <summary>
    /// Represents the type of URL match found
    /// </summary>
    public enum MatchType
    {
        /// <summary>
        /// No matching rule or group found
        /// </summary>
        NoMatch,

        /// <summary>
        /// Matched an individual URL rule
        /// </summary>
        IndividualRule,

        /// <summary>
        /// Matched a URL group's default settings
        /// </summary>
        UrlGroup,

        /// <summary>
        /// Matched a URL group override (specific URL within a group)
        /// </summary>
        GroupOverride
    }

    /// <summary>
    /// Contains the result of a URL matching operation
    /// </summary>
    public class MatchResult
    {
        public MatchType Type { get; set; } = MatchType.NoMatch;
        public UrlRule? Rule { get; set; }
        public UrlGroup? Group { get; set; }
        public UrlGroupOverride? Override { get; set; }

        /// <summary>
        /// Gets the effective browser name based on match type
        /// </summary>
        public string GetBrowserName()
        {
            return Type switch
            {
                MatchType.IndividualRule => Rule?.DisplayBrowserName ?? string.Empty,
                MatchType.GroupOverride => Override?.BrowserName ?? string.Empty,
                MatchType.UrlGroup => Group?.DefaultBrowserName ?? string.Empty,
                _ => string.Empty
            };
        }

        /// <summary>
        /// Gets the effective browser path based on match type
        /// </summary>
        public string GetBrowserPath()
        {
            return Type switch
            {
                MatchType.IndividualRule => Rule?.FirstProfile?.BrowserPath ?? Rule?.BrowserPath ?? string.Empty,
                MatchType.GroupOverride => Override?.BrowserPath ?? string.Empty,
                MatchType.UrlGroup => Group?.DefaultBrowserPath ?? string.Empty,
                _ => string.Empty
            };
        }

        /// <summary>
        /// Gets the effective profile name based on match type
        /// </summary>
        public string GetProfileName()
        {
            return Type switch
            {
                MatchType.IndividualRule => Rule?.FirstProfile?.ProfileName ?? Rule?.ProfileName ?? string.Empty,
                MatchType.GroupOverride => Override?.ProfileName ?? string.Empty,
                MatchType.UrlGroup => Group?.DefaultProfileName ?? string.Empty,
                _ => string.Empty
            };
        }

        /// <summary>
        /// Gets the effective profile arguments based on match type
        /// </summary>
        public string GetProfileArguments()
        {
            return Type switch
            {
                MatchType.IndividualRule => Rule?.FirstProfile?.ProfileArguments ?? Rule?.ProfileArguments ?? string.Empty,
                MatchType.GroupOverride => Override?.ProfileArguments ?? string.Empty,
                MatchType.UrlGroup => Group?.DefaultProfileArguments ?? string.Empty,
                _ => string.Empty
            };
        }

        /// <summary>
        /// Gets the effective behavior (for groups/overrides)
        /// </summary>
        public UrlGroupBehavior GetBehavior()
        {
            return Type switch
            {
                MatchType.GroupOverride => Override?.Behavior ?? UrlGroupBehavior.UseDefault,
                MatchType.UrlGroup => Group?.Behavior ?? UrlGroupBehavior.UseDefault,
                _ => UrlGroupBehavior.UseDefault
            };
        }

        /// <summary>
        /// Gets the linked profile group ID (for groups/overrides with ShowProfilePicker behavior)
        /// </summary>
        public string GetLinkedProfileGroupId()
        {
            return Type switch
            {
                MatchType.GroupOverride => Override?.LinkedProfileGroupId ?? string.Empty,
                MatchType.UrlGroup => Group?.LinkedProfileGroupId ?? string.Empty,
                _ => string.Empty
            };
        }

        /// <summary>
        /// Returns true if this match should show a profile picker
        /// </summary>
        public bool ShouldShowProfilePicker()
        {
            // Individual rules with multiple profiles should show picker
            if (Type == MatchType.IndividualRule && Rule != null && Rule.HasMultipleProfiles)
            {
                return true;
            }

            return false;
        }
    }
}