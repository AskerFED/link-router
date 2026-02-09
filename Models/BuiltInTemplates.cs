using System;
using System.Collections.Generic;

namespace BrowserSelector.Models
{
    /// <summary>
    /// Manifest for built-in URL group templates.
    /// Contains version information and all template definitions.
    /// </summary>
    public class BuiltInTemplateManifest
    {
        /// <summary>
        /// Version of the template manifest (e.g., "1.0.0", "1.1.0").
        /// Used to determine if updates need to be applied.
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// When this manifest was last updated.
        /// </summary>
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// List of built-in URL group templates.
        /// </summary>
        public List<BuiltInTemplate> Templates { get; set; } = new List<BuiltInTemplate>();
    }

    /// <summary>
    /// Represents a built-in URL group template.
    /// </summary>
    public class BuiltInTemplate
    {
        /// <summary>
        /// Stable unique identifier for this template (e.g., "builtin-m365").
        /// Must match the Id used in UrlGroup for built-in groups.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the group.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of what this group is for.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// URL patterns included in this template.
        /// </summary>
        public List<BuiltInPatternEntry> Patterns { get; set; } = new List<BuiltInPatternEntry>();
    }

    /// <summary>
    /// Represents a single URL pattern entry in a built-in template.
    /// </summary>
    public class BuiltInPatternEntry
    {
        /// <summary>
        /// Stable unique identifier for this pattern (e.g., "m365-teams").
        /// Used to track which patterns have been added/deleted by the user.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The URL pattern to match (e.g., "teams.microsoft.com").
        /// </summary>
        public string Pattern { get; set; } = string.Empty;

        /// <summary>
        /// Version when this pattern was added (e.g., "1.0.0").
        /// Used to determine if this is a new pattern that should be added on update.
        /// </summary>
        public string VersionAdded { get; set; } = "1.0.0";
    }
}
