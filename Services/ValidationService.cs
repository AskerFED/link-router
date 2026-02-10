using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrowserSelector.Models;

namespace BrowserSelector.Services
{
    /// <summary>
    /// Validation error codes.
    /// </summary>
    public static class ValidationCodes
    {
        // Pattern Errors (Block Save)
        public const string PATTERN_EMPTY = "PATTERN_EMPTY";
        public const string PATTERN_INVALID_FORMAT = "PATTERN_INVALID_FORMAT";
        public const string PATTERN_CONSECUTIVE_DOTS = "PATTERN_CONSECUTIVE_DOTS";
        public const string PATTERN_UNSUPPORTED_WILDCARD = "PATTERN_UNSUPPORTED_WILDCARD";
        public const string PATTERN_UNSUPPORTED_SCHEME = "PATTERN_UNSUPPORTED_SCHEME";
        public const string PATTERN_EXACT_DUPLICATE = "PATTERN_EXACT_DUPLICATE";
        public const string PATTERN_SAME_DOMAIN_PROFILE = "PATTERN_SAME_DOMAIN_PROFILE";

        // Pattern Warnings (Confirm)
        public const string PATTERN_CONFLICT_DIFFERENT_PROFILE = "PATTERN_CONFLICT_DIFFERENT_PROFILE";
        public const string PATTERN_PARENT_DOMAIN_EXISTS = "PATTERN_PARENT_DOMAIN_EXISTS";
        public const string PATTERN_SUBDOMAIN_EXISTS = "PATTERN_SUBDOMAIN_EXISTS";
        public const string PATTERN_EXISTS_IN_GROUP = "PATTERN_EXISTS_IN_GROUP";

        // Group Errors (Block Save)
        public const string GROUP_NAME_EMPTY = "GROUP_NAME_EMPTY";
        public const string GROUP_NAME_DUPLICATE = "GROUP_NAME_DUPLICATE";
        public const string GROUP_PATTERNS_EMPTY = "GROUP_PATTERNS_EMPTY";

        // Group Warnings (Confirm)
        public const string GROUP_PATTERN_EXISTS_AS_RULE = "GROUP_PATTERN_EXISTS_AS_RULE";
        public const string GROUP_PATTERN_OVERLAP = "GROUP_PATTERN_OVERLAP";

        // Profile Errors
        public const string PROFILE_REQUIRED = "PROFILE_REQUIRED";
        public const string BROWSER_NOT_FOUND = "BROWSER_NOT_FOUND";
        public const string BROWSER_PATH_INVALID = "BROWSER_PATH_INVALID";
    }

    /// <summary>
    /// Centralized validation service for URL rules and groups.
    /// </summary>
    public static class ValidationService
    {
        #region Pattern Normalization

        /// <summary>
        /// Normalizes a URL pattern for consistent storage and comparison.
        /// </summary>
        public static string NormalizePattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return string.Empty;

            var normalized = pattern.Trim();
            normalized = StripProtocol(normalized);
            normalized = RemoveTrailingSlash(normalized);
            normalized = normalized.ToLowerInvariant();

            return normalized;
        }

        private static string StripProtocol(string pattern)
        {
            if (pattern.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                return pattern.Substring(7);
            if (pattern.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return pattern.Substring(8);
            return pattern;
        }

        private static string RemoveTrailingSlash(string pattern) =>
            pattern.TrimEnd('/');

        /// <summary>
        /// Extracts the domain portion from a pattern.
        /// </summary>
        public static string ExtractDomain(string pattern)
        {
            var normalized = NormalizePattern(pattern);
            var slashIndex = normalized.IndexOf('/');
            return slashIndex > 0 ? normalized.Substring(0, slashIndex) : normalized;
        }

        #endregion

        #region Pattern Validation

        /// <summary>
        /// Validates just the URL pattern format (for real-time validation).
        /// </summary>
        public static ValidationResult ValidatePattern(string pattern)
        {
            var result = new ValidationResult();

            // Empty check
            if (string.IsNullOrWhiteSpace(pattern))
            {
                result.Errors.Add(new ValidationMessage
                {
                    Code = ValidationCodes.PATTERN_EMPTY,
                    Severity = ValidationSeverity.Error,
                    Message = "URL pattern cannot be empty.",
                    FieldName = "Pattern"
                });
                return result;
            }

            // Normalize
            var normalized = NormalizePattern(pattern);
            result.NormalizedValue = normalized;

            // Check for wildcards (not supported)
            if (pattern.Contains("*"))
            {
                result.Errors.Add(new ValidationMessage
                {
                    Code = ValidationCodes.PATTERN_UNSUPPORTED_WILDCARD,
                    Severity = ValidationSeverity.Error,
                    Message = "Wildcards (*) are not supported. Use partial domain matching instead.",
                    FieldName = "Pattern"
                });
            }

            // Check for consecutive dots
            if (normalized.Contains(".."))
            {
                result.Errors.Add(new ValidationMessage
                {
                    Code = ValidationCodes.PATTERN_CONSECUTIVE_DOTS,
                    Severity = ValidationSeverity.Error,
                    Message = "Pattern contains consecutive dots (..).",
                    FieldName = "Pattern"
                });
            }

            // Check for unsupported schemes
            if (pattern.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
                pattern.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ||
                pattern.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add(new ValidationMessage
                {
                    Code = ValidationCodes.PATTERN_UNSUPPORTED_SCHEME,
                    Severity = ValidationSeverity.Error,
                    Message = "Only http and https URLs are supported.",
                    FieldName = "Pattern"
                });
            }

            // Check for spaces
            if (normalized.Contains(" "))
            {
                result.Errors.Add(new ValidationMessage
                {
                    Code = ValidationCodes.PATTERN_INVALID_FORMAT,
                    Severity = ValidationSeverity.Error,
                    Message = "Pattern cannot contain spaces.",
                    FieldName = "Pattern"
                });
            }

            return result;
        }

        /// <summary>
        /// Validates a pattern for adding to a group.
        /// Checks format, if pattern exists as individual rule, and if pattern exists in other groups.
        /// </summary>
        /// <param name="pattern">The pattern to validate</param>
        /// <param name="currentGroupId">Optional: The ID of the group being edited (to exclude from overlap check)</param>
        public static ValidationResult ValidatePatternForGroup(string pattern, string? currentGroupId = null)
        {
            // First validate format
            var result = ValidatePattern(pattern);
            if (!result.IsValid)
                return result;

            var normalizedPattern = result.NormalizedValue ?? pattern;

            // Check if pattern exists as individual rule
            var allRules = UrlRuleManager.LoadRules();
            var matchingRule = allRules.FirstOrDefault(r =>
                NormalizePattern(r.Pattern) == normalizedPattern);

            if (matchingRule != null)
            {
                result.Warnings.Add(new ValidationMessage
                {
                    Code = ValidationCodes.GROUP_PATTERN_EXISTS_AS_RULE,
                    Severity = ValidationSeverity.Warning,
                    Message = "This pattern exists as an individual rule and will override the group.",
                    Details = $"Individual rule: {matchingRule.Pattern}",
                    ConflictingEntity = matchingRule.Pattern
                });
            }

            // Check if pattern exists in other groups
            var allGroups = UrlGroupManager.LoadGroups()
                .Where(g => currentGroupId == null || g.Id != currentGroupId);

            foreach (var otherGroup in allGroups)
            {
                var matchingPattern = otherGroup.UrlPatterns?
                    .FirstOrDefault(p => NormalizePattern(p.Pattern) == normalizedPattern);

                if (matchingPattern != null)
                {
                    result.Warnings.Add(new ValidationMessage
                    {
                        Code = ValidationCodes.GROUP_PATTERN_OVERLAP,
                        Severity = ValidationSeverity.Warning,
                        Message = $"This pattern already exists in group '{otherGroup.Name}'.",
                        Details = $"Group: {otherGroup.Name}",
                        ConflictingEntity = otherGroup.Name
                    });
                    break; // Only report first conflict
                }
            }

            return result;
        }

        #endregion

        #region Individual Rule Validation

        /// <summary>
        /// Validates an individual URL rule.
        /// </summary>
        public static ValidationResult ValidateIndividualRule(
            string pattern,
            List<RuleProfile> profiles,
            string? existingRuleId = null,
            string? excludeGroupId = null)
        {
            var result = new ValidationResult();

            // 1. Validate pattern format
            var patternResult = ValidatePattern(pattern);
            if (!patternResult.IsValid)
            {
                result.Errors.AddRange(patternResult.Errors);
                return result;
            }

            var normalizedPattern = patternResult.NormalizedValue ?? pattern;
            result.NormalizedValue = normalizedPattern;

            // 2. Validate profile selection
            if (profiles == null || profiles.Count == 0)
            {
                result.Errors.Add(new ValidationMessage
                {
                    Code = ValidationCodes.PROFILE_REQUIRED,
                    Severity = ValidationSeverity.Error,
                    Message = "Please select a browser and profile.",
                    FieldName = "Profile"
                });
                return result;
            }

            // 3. Validate browser paths exist
            var browserValidation = ValidateProfiles(profiles);
            result.Warnings.AddRange(browserValidation.Warnings);

            // 4. Check for conflicts
            var conflicts = FindConflicts(normalizedPattern, existingRuleId, excludeGroupId);

            // Exact duplicate = Error
            if (conflicts.HasExactDuplicate && conflicts.DuplicateRule != null)
            {
                result.Errors.Add(new ValidationMessage
                {
                    Code = ValidationCodes.PATTERN_EXACT_DUPLICATE,
                    Severity = ValidationSeverity.Error,
                    Message = "This exact pattern already exists in another rule.",
                    FieldName = "Pattern",
                    ConflictingEntity = conflicts.DuplicateRule.Pattern
                });
                return result;
            }

            // Same domain + same profile combo = Error
            var sameProfileConflict = conflicts.ConflictingRules
                .Where(r => ProfilesMatch(r.Profiles, profiles))
                .FirstOrDefault();
            if (sameProfileConflict != null)
            {
                result.Errors.Add(new ValidationMessage
                {
                    Code = ValidationCodes.PATTERN_SAME_DOMAIN_PROFILE,
                    Severity = ValidationSeverity.Error,
                    Message = "A rule with this domain and same browser/profile already exists.",
                    FieldName = "Pattern",
                    ConflictingEntity = sameProfileConflict.Pattern
                });
                return result;
            }

            // Same domain + different profile = Warning
            var differentProfileConflict = conflicts.ConflictingRules
                .Where(r => !ProfilesMatch(r.Profiles, profiles))
                .FirstOrDefault();
            if (differentProfileConflict != null)
            {
                result.Warnings.Add(new ValidationMessage
                {
                    Code = ValidationCodes.PATTERN_CONFLICT_DIFFERENT_PROFILE,
                    Severity = ValidationSeverity.Warning,
                    Message = "A rule for this domain exists with a different browser/profile.",
                    Details = $"Existing rule: {differentProfileConflict.Pattern}",
                    ConflictingEntity = differentProfileConflict.Pattern
                });
            }

            // Hierarchy conflicts = Warning
            foreach (var hierarchy in conflicts.HierarchyConflicts)
            {
                var code = hierarchy.Type == HierarchyConflictType.ParentDomainExists
                    ? ValidationCodes.PATTERN_PARENT_DOMAIN_EXISTS
                    : ValidationCodes.PATTERN_SUBDOMAIN_EXISTS;

                var message = hierarchy.Type == HierarchyConflictType.ParentDomainExists
                    ? "A parent domain rule already exists and will take precedence."
                    : "Subdomain rules exist that may override this pattern.";

                result.Warnings.Add(new ValidationMessage
                {
                    Code = code,
                    Severity = ValidationSeverity.Warning,
                    Message = message,
                    Details = $"Pattern: {hierarchy.ExistingPattern}",
                    ConflictingEntity = hierarchy.EntityName
                });
            }

            // Check if pattern exists in a group = Warning
            if (conflicts.ConflictingGroups.Any())
            {
                var group = conflicts.ConflictingGroups.First();
                result.Warnings.Add(new ValidationMessage
                {
                    Code = ValidationCodes.PATTERN_EXISTS_IN_GROUP,
                    Severity = ValidationSeverity.Warning,
                    Message = "The group rules will be overridden by the individual rule.",
                    Details = $"Group: {group.Name}",
                    ConflictingEntity = group.Name
                });
            }

            return result;
        }

        #endregion

        #region URL Group Validation

        /// <summary>
        /// Validates a URL group.
        /// </summary>
        public static ValidationResult ValidateUrlGroup(
            string name,
            List<string> patterns,
            List<RuleProfile> profiles,
            string? existingGroupId = null)
        {
            var result = new ValidationResult();

            // 1. Validate group name
            var nameResult = ValidateGroupName(name, existingGroupId);
            result.Merge(nameResult);
            if (!nameResult.IsValid)
            {
                return result;
            }

            // 2. Validate patterns exist
            if (patterns == null || patterns.Count == 0)
            {
                result.Errors.Add(new ValidationMessage
                {
                    Code = ValidationCodes.GROUP_PATTERNS_EMPTY,
                    Severity = ValidationSeverity.Error,
                    Message = "At least one URL pattern is required.",
                    FieldName = "Patterns"
                });
                return result;
            }

            // 3. Validate each pattern format
            foreach (var pattern in patterns)
            {
                var patternResult = ValidatePattern(pattern);
                if (!patternResult.IsValid)
                {
                    foreach (var error in patternResult.Errors)
                    {
                        error.Details = $"Pattern: {pattern}";
                        result.Errors.Add(error);
                    }
                }
            }

            if (!result.IsValid)
                return result;

            // 4. Validate profile selection
            if (profiles == null || profiles.Count == 0)
            {
                result.Errors.Add(new ValidationMessage
                {
                    Code = ValidationCodes.PROFILE_REQUIRED,
                    Severity = ValidationSeverity.Error,
                    Message = "Please select a browser and profile.",
                    FieldName = "Profile"
                });
                return result;
            }

            // 5. Validate browser paths exist
            var browserValidation = ValidateProfiles(profiles);
            result.Warnings.AddRange(browserValidation.Warnings);

            // 6. Check for conflicts with individual rules
            var allRules = UrlRuleManager.LoadRules();
            foreach (var pattern in patterns)
            {
                var normalizedPattern = NormalizePattern(pattern);
                var matchingRule = allRules.FirstOrDefault(r =>
                    NormalizePattern(r.Pattern) == normalizedPattern);

                if (matchingRule != null)
                {
                    result.Warnings.Add(new ValidationMessage
                    {
                        Code = ValidationCodes.GROUP_PATTERN_EXISTS_AS_RULE,
                        Severity = ValidationSeverity.Warning,
                        Message = $"Pattern '{pattern}' exists as an individual rule.",
                        Details = $"Rule pattern: {matchingRule.Pattern}",
                        ConflictingEntity = matchingRule.Pattern
                    });
                }
            }

            // 7. Check for conflicts with other groups
            var allGroups = UrlGroupManager.LoadGroups()
                .Where(g => existingGroupId == null || g.Id != existingGroupId);

            foreach (var pattern in patterns)
            {
                var normalizedPattern = NormalizePattern(pattern);
                foreach (var otherGroup in allGroups)
                {
                    var matchingPattern = otherGroup.UrlPatterns?
                        .FirstOrDefault(p => NormalizePattern(p.Pattern) == normalizedPattern);

                    if (matchingPattern != null)
                    {
                        result.Warnings.Add(new ValidationMessage
                        {
                            Code = ValidationCodes.GROUP_PATTERN_OVERLAP,
                            Severity = ValidationSeverity.Warning,
                            Message = $"Pattern '{pattern}' overlaps with group '{otherGroup.Name}'.",
                            Details = $"Group: {otherGroup.Name}",
                            ConflictingEntity = otherGroup.Name
                        });
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Validates group name uniqueness.
        /// </summary>
        public static ValidationResult ValidateGroupName(string name, string? existingGroupId = null)
        {
            var result = new ValidationResult();

            // Empty check
            if (string.IsNullOrWhiteSpace(name))
            {
                result.Errors.Add(new ValidationMessage
                {
                    Code = ValidationCodes.GROUP_NAME_EMPTY,
                    Severity = ValidationSeverity.Error,
                    Message = "Group name cannot be empty.",
                    FieldName = "Name"
                });
                return result;
            }

            // Duplicate check (case-insensitive)
            var allGroups = UrlGroupManager.LoadGroups();
            var duplicateGroup = allGroups.FirstOrDefault(g =>
                g.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase) &&
                (existingGroupId == null || g.Id != existingGroupId));

            if (duplicateGroup != null)
            {
                result.Errors.Add(new ValidationMessage
                {
                    Code = ValidationCodes.GROUP_NAME_DUPLICATE,
                    Severity = ValidationSeverity.Error,
                    Message = "A group with this name already exists.",
                    FieldName = "Name",
                    ConflictingEntity = duplicateGroup.Name
                });
            }

            return result;
        }

        #endregion

        #region Conflict Detection

        /// <summary>
        /// Finds conflicting rules/groups for a pattern.
        /// </summary>
        public static ConflictCheckResult FindConflicts(
            string pattern,
            string? excludeRuleId = null,
            string? excludeGroupId = null)
        {
            var result = new ConflictCheckResult();
            var normalizedPattern = NormalizePattern(pattern);
            var patternDomain = ExtractDomain(pattern);

            // Check individual rules
            var rules = UrlRuleManager.LoadRules()
                .Where(r => excludeRuleId == null || r.Id != excludeRuleId);

            foreach (var rule in rules)
            {
                var ruleNormalized = NormalizePattern(rule.Pattern);
                var ruleDomain = ExtractDomain(rule.Pattern);

                // Exact duplicate
                if (ruleNormalized == normalizedPattern)
                {
                    result.HasExactDuplicate = true;
                    result.DuplicateRule = rule;
                    return result;
                }

                // Same domain (conflicting)
                if (ruleDomain == patternDomain)
                {
                    result.ConflictingRules.Add(rule);
                }

                // Hierarchy check
                if (IsParentDomain(ruleDomain, patternDomain))
                {
                    result.HierarchyConflicts.Add(new HierarchyConflict
                    {
                        Type = HierarchyConflictType.ParentDomainExists,
                        ExistingPattern = rule.Pattern,
                        RuleId = rule.Id,
                        EntityName = rule.Pattern
                    });
                }
                else if (IsParentDomain(patternDomain, ruleDomain))
                {
                    result.HierarchyConflicts.Add(new HierarchyConflict
                    {
                        Type = HierarchyConflictType.SubdomainExists,
                        ExistingPattern = rule.Pattern,
                        RuleId = rule.Id,
                        EntityName = rule.Pattern
                    });
                }
            }

            // Check URL groups
            var groups = UrlGroupManager.LoadGroups()
                .Where(g => excludeGroupId == null || g.Id != excludeGroupId);

            foreach (var group in groups)
            {
                if (group.UrlPatterns == null) continue;

                foreach (var groupPattern in group.UrlPatterns)
                {
                    var groupNormalized = NormalizePattern(groupPattern.Pattern);

                    if (groupNormalized == normalizedPattern)
                    {
                        result.ConflictingGroups.Add(group);
                        break;
                    }
                }
            }

            return result;
        }

        private static bool IsParentDomain(string potentialParent, string potentialChild)
        {
            if (string.IsNullOrEmpty(potentialParent) || string.IsNullOrEmpty(potentialChild))
                return false;

            // google.com is parent of mail.google.com
            return potentialChild.EndsWith("." + potentialParent, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ProfilesMatch(List<RuleProfile>? profiles1, List<RuleProfile>? profiles2)
        {
            if (profiles1 == null || profiles2 == null)
                return false;

            // Check if any profile matches by browser path + profile path
            return profiles1.Any(p1 =>
                profiles2.Any(p2 =>
                    p1.BrowserPath == p2.BrowserPath &&
                    p1.ProfilePath == p2.ProfilePath));
        }

        #endregion

        #region Profile Validation

        /// <summary>
        /// Validates that all profiles have valid browser paths.
        /// </summary>
        public static ValidationResult ValidateProfiles(List<RuleProfile> profiles)
        {
            var result = new ValidationResult();

            if (profiles == null || profiles.Count == 0)
                return result;

            var checkedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var profile in profiles)
            {
                if (string.IsNullOrEmpty(profile.BrowserPath))
                    continue;

                // Avoid checking the same path multiple times
                if (checkedPaths.Contains(profile.BrowserPath))
                    continue;

                checkedPaths.Add(profile.BrowserPath);

                if (!File.Exists(profile.BrowserPath))
                {
                    result.Warnings.Add(new ValidationMessage
                    {
                        Code = ValidationCodes.BROWSER_PATH_INVALID,
                        Severity = ValidationSeverity.Warning,
                        Message = $"Browser executable not found at: {profile.BrowserPath}",
                        Details = $"Browser: {profile.BrowserName}",
                        ConflictingEntity = profile.BrowserName
                    });
                }
            }

            return result;
        }

        #endregion
    }
}
