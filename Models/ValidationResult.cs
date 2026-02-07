using System.Collections.Generic;
using System.Linq;

namespace BrowserSelector.Models
{
    /// <summary>
    /// Severity levels for validation messages.
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Represents a single validation message with severity and details.
    /// </summary>
    public class ValidationMessage
    {
        public string Code { get; set; } = string.Empty;
        public ValidationSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? FieldName { get; set; }
        public string? ConflictingEntity { get; set; }

        // Helper properties for UI binding
        public bool IsError => Severity == ValidationSeverity.Error;
        public bool IsWarning => Severity == ValidationSeverity.Warning;
        public bool IsInfo => Severity == ValidationSeverity.Info;
    }

    /// <summary>
    /// Result of a validation operation, containing errors, warnings, and info messages.
    /// </summary>
    public class ValidationResult
    {
        public List<ValidationMessage> Errors { get; set; } = new();
        public List<ValidationMessage> Warnings { get; set; } = new();
        public List<ValidationMessage> Infos { get; set; } = new();
        public string? NormalizedValue { get; set; }

        /// <summary>
        /// Returns true if there are no errors (warnings are allowed).
        /// </summary>
        public bool IsValid => !Errors.Any();

        /// <summary>
        /// Returns true if there are any warning messages.
        /// </summary>
        public bool HasWarnings => Warnings.Any();

        /// <summary>
        /// Returns true if the operation can proceed (valid, possibly with warnings).
        /// </summary>
        public bool CanProceedWithWarnings => IsValid;

        /// <summary>
        /// Gets all messages combined (errors, warnings, infos).
        /// </summary>
        public IEnumerable<ValidationMessage> GetAllMessages() =>
            Errors.Concat(Warnings).Concat(Infos);

        /// <summary>
        /// Creates a successful validation result with optional normalized value.
        /// </summary>
        public static ValidationResult Success(string? normalizedValue = null) =>
            new() { NormalizedValue = normalizedValue };

        /// <summary>
        /// Creates a validation result with a single error.
        /// </summary>
        public static ValidationResult Error(string code, string message, string? field = null) =>
            new()
            {
                Errors =
                {
                    new ValidationMessage
                    {
                        Code = code,
                        Severity = ValidationSeverity.Error,
                        Message = message,
                        FieldName = field
                    }
                }
            };

        /// <summary>
        /// Creates a validation result with a single warning.
        /// </summary>
        public static ValidationResult Warning(string code, string message, string? details = null) =>
            new()
            {
                Warnings =
                {
                    new ValidationMessage
                    {
                        Code = code,
                        Severity = ValidationSeverity.Warning,
                        Message = message,
                        Details = details
                    }
                }
            };

        /// <summary>
        /// Merges another validation result into this one.
        /// </summary>
        public void Merge(ValidationResult other)
        {
            Errors.AddRange(other.Errors);
            Warnings.AddRange(other.Warnings);
            Infos.AddRange(other.Infos);
            if (string.IsNullOrEmpty(NormalizedValue) && !string.IsNullOrEmpty(other.NormalizedValue))
            {
                NormalizedValue = other.NormalizedValue;
            }
        }
    }

    /// <summary>
    /// Result of conflict checking for a pattern.
    /// </summary>
    public class ConflictCheckResult
    {
        public bool HasExactDuplicate { get; set; }
        public UrlRule? DuplicateRule { get; set; }
        public UrlGroup? DuplicateGroup { get; set; }
        public List<UrlRule> ConflictingRules { get; set; } = new();
        public List<UrlGroup> ConflictingGroups { get; set; } = new();
        public List<HierarchyConflict> HierarchyConflicts { get; set; } = new();
    }

    /// <summary>
    /// Represents a domain hierarchy conflict (parent/subdomain).
    /// </summary>
    public class HierarchyConflict
    {
        public HierarchyConflictType Type { get; set; }
        public string ExistingPattern { get; set; } = string.Empty;
        public string? RuleId { get; set; }
        public string? GroupId { get; set; }
        public string EntityName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Types of domain hierarchy conflicts.
    /// </summary>
    public enum HierarchyConflictType
    {
        /// <summary>
        /// A parent domain rule already exists (e.g., adding mail.google.com when google.com exists).
        /// </summary>
        ParentDomainExists,

        /// <summary>
        /// A subdomain rule already exists (e.g., adding google.com when mail.google.com exists).
        /// </summary>
        SubdomainExists,

        /// <summary>
        /// Patterns overlap and would match the same URLs.
        /// </summary>
        OverlappingPattern
    }
}
