using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using BrowserSelector.Models;

namespace BrowserSelector.Services
{
    /// <summary>
    /// Service that monitors the Windows clipboard for URL changes.
    /// Uses Win32 AddClipboardFormatListener for efficient clipboard monitoring.
    /// </summary>
    public class ClipboardMonitorService : IDisposable
    {
        #region Win32 Interop

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private const int WM_CLIPBOARDUPDATE = 0x031D;

        #endregion

        #region Singleton

        private static ClipboardMonitorService? _instance;
        private static readonly object _lock = new object();

        public static ClipboardMonitorService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ClipboardMonitorService();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired when a valid URL is detected in the clipboard that matches a rule with notifications enabled.
        /// </summary>
        public event EventHandler<ClipboardUrlEventArgs>? UrlDetected;

        #endregion

        #region Fields

        private HwndSource? _hwndSource;
        private bool _isRunning;
        private readonly ConcurrentDictionary<string, DateTime> _recentDomains = new();
        private string? _lastClipboardText;
        private bool _disposed;

        #endregion

        #region Properties

        public bool IsRunning => _isRunning;

        #endregion

        #region Constructor

        private ClipboardMonitorService()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts monitoring the clipboard for URL changes.
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;

            Logger.Log("ClipboardMonitorService: Starting clipboard monitoring");

            Application.Current.Dispatcher.Invoke(() =>
            {
                // Create a hidden window to receive clipboard messages
                var parameters = new HwndSourceParameters("ClipboardMonitorWindow")
                {
                    Width = 0,
                    Height = 0,
                    PositionX = 0,
                    PositionY = 0,
                    WindowStyle = 0x800000 // WS_SYSMENU - minimal window
                };

                _hwndSource = new HwndSource(parameters);
                _hwndSource.AddHook(WndProc);

                if (AddClipboardFormatListener(_hwndSource.Handle))
                {
                    _isRunning = true;
                    Logger.Log("ClipboardMonitorService: Clipboard listener registered successfully");
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    Logger.Log($"ClipboardMonitorService: Failed to register clipboard listener. Error: {error}");
                }
            });
        }

        /// <summary>
        /// Stops monitoring the clipboard.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;

            Logger.Log("ClipboardMonitorService: Stopping clipboard monitoring");

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_hwndSource != null)
                {
                    RemoveClipboardFormatListener(_hwndSource.Handle);
                    _hwndSource.RemoveHook(WndProc);
                    _hwndSource.Dispose();
                    _hwndSource = null;
                }

                _isRunning = false;
                Logger.Log("ClipboardMonitorService: Clipboard monitoring stopped");
            });
        }

        /// <summary>
        /// Clears the cooldown cache, allowing immediate notifications.
        /// </summary>
        public void ClearCooldownCache()
        {
            _recentDomains.Clear();
        }

        #endregion

        #region Private Methods

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                handled = true;
                OnClipboardUpdate();
            }

            return IntPtr.Zero;
        }

        private void OnClipboardUpdate()
        {
            try
            {
                // Get clipboard text
                string? text = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Clipboard.ContainsText())
                    {
                        text = Clipboard.GetText();
                    }
                });

                if (string.IsNullOrWhiteSpace(text))
                    return;

                // Avoid processing the same text twice
                if (text == _lastClipboardText)
                    return;

                _lastClipboardText = text;

                // Check if it's a valid URL
                var url = NormalizeUrl(text.Trim());
                if (url == null)
                    return;

                Logger.Log($"ClipboardMonitorService: Valid URL detected: {url}");

                // Get settings
                var settings = SettingsManager.LoadSettings().ClipboardMonitoring;

                // Check if paused
                if (settings.IsPaused)
                {
                    Logger.Log("ClipboardMonitorService: Monitoring is paused, ignoring URL");
                    return;
                }

                // Extract domain
                string domain;
                try
                {
                    domain = new Uri(url).Host.ToLowerInvariant();
                }
                catch
                {
                    return;
                }

                // Check cooldown
                if (!ShouldNotify(domain, settings.CooldownSeconds))
                {
                    Logger.Log($"ClipboardMonitorService: Domain '{domain}' is in cooldown period");
                    return;
                }

                // Check if URL matches a rule
                var match = UrlRuleManager.FindMatch(url);
                if (match.Type == MatchType.NoMatch)
                {
                    // Also check if a disabled rule would have matched - suppress notification
                    bool hasDisabledMatch = UrlRuleManager.HasDisabledMatchingRule(url) ||
                                            UrlGroupManager.HasDisabledMatchingGroup(url);
                    if (hasDisabledMatch)
                    {
                        Logger.Log($"ClipboardMonitorService: Disabled rule/group exists for URL, suppressing notification");
                    }
                    else
                    {
                        Logger.Log($"ClipboardMonitorService: URL doesn't match any rule, ignoring");
                    }
                    return;
                }

                // Check if the matched rule has clipboard notifications enabled
                bool clipboardEnabled = match.Type switch
                {
                    MatchType.IndividualRule => match.Rule?.ClipboardNotificationsEnabled ?? true,
                    MatchType.UrlGroup => match.Group?.ClipboardNotificationsEnabled ?? true,
                    MatchType.GroupOverride => match.Group?.ClipboardNotificationsEnabled ?? true,
                    _ => false
                };

                if (!clipboardEnabled)
                {
                    Logger.Log($"ClipboardMonitorService: Rule has clipboard notifications disabled");
                    return;
                }

                // Fire event
                UrlDetected?.Invoke(this, new ClipboardUrlEventArgs(url, domain));
            }
            catch (Exception ex)
            {
                Logger.Log($"ClipboardMonitorService: Error processing clipboard: {ex.Message}");
            }
        }

        private string? NormalizeUrl(string text)
        {
            // Skip if text is too long (likely not a URL)
            if (text.Length > 2000)
                return null;

            // Skip if text contains newlines (likely multi-line text)
            if (text.Contains('\n') || text.Contains('\r'))
                return null;

            // Check for common URL patterns
            string url = text;

            // Handle www. prefix without protocol
            if (url.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            // Must start with http:// or https://
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Validate as URI
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;

            // Must be HTTP or HTTPS
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return null;

            // Must have a valid host
            if (string.IsNullOrWhiteSpace(uri.Host))
                return null;

            // Host must contain a dot (filter out localhost, etc. if desired)
            // But allow localhost for development
            if (!uri.Host.Contains('.') && uri.Host != "localhost")
                return null;

            return url;
        }

        private bool ShouldNotify(string domain, int cooldownSeconds)
        {
            var now = DateTime.Now;

            // Clean up old entries
            var cutoff = now.AddMinutes(-5);
            foreach (var key in _recentDomains.Keys)
            {
                if (_recentDomains.TryGetValue(key, out var time) && time < cutoff)
                {
                    _recentDomains.TryRemove(key, out _);
                }
            }

            // Check cooldown
            if (_recentDomains.TryGetValue(domain, out var lastNotified))
            {
                if ((now - lastNotified).TotalSeconds < cooldownSeconds)
                {
                    return false;
                }
            }

            // Update last notified time
            _recentDomains[domain] = now;
            return true;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            Stop();
            _disposed = true;
        }

        #endregion
    }

    /// <summary>
    /// Event args for clipboard URL detection events.
    /// </summary>
    public class ClipboardUrlEventArgs : EventArgs
    {
        public string Url { get; }
        public string Domain { get; }

        public ClipboardUrlEventArgs(string url, string domain)
        {
            Url = url;
            Domain = domain;
        }
    }
}
