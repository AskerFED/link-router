using System;

namespace BrowserSelector.Models
{
    /// <summary>
    /// Settings for clipboard URL monitoring feature.
    /// Simplified: Global enable/disable only. Per-rule control is in UrlRule/UrlGroup.
    /// </summary>
    public class ClipboardSettings
    {
        /// <summary>
        /// Whether clipboard monitoring is enabled globally. Enabled by default.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Internal cooldown in seconds between notifications for the same domain.
        /// Prevents notification spam when copying multiple URLs.
        /// </summary>
        public int CooldownSeconds { get; set; } = 5;

        /// <summary>
        /// When set, clipboard monitoring is paused until this time.
        /// Null means monitoring is active (not paused).
        /// </summary>
        public DateTime? PauseEndTime { get; set; }

        /// <summary>
        /// Checks if monitoring is currently paused.
        /// </summary>
        public bool IsPaused => PauseEndTime.HasValue && DateTime.Now < PauseEndTime.Value;

        /// <summary>
        /// Gets the remaining pause time, or null if not paused.
        /// </summary>
        public TimeSpan? RemainingPauseTime
        {
            get
            {
                if (!PauseEndTime.HasValue) return null;
                var remaining = PauseEndTime.Value - DateTime.Now;
                return remaining > TimeSpan.Zero ? remaining : null;
            }
        }
    }
}
