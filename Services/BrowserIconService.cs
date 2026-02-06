using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BrowserSelector.Services
{
    /// <summary>
    /// Service for extracting and caching browser icons from executable files.
    /// </summary>
    public static class BrowserIconService
    {
        private static readonly Dictionary<string, ImageSource> _iconCache = new Dictionary<string, ImageSource>();
        private static readonly object _cacheLock = new object();

        /// <summary>
        /// Gets the icon for a browser executable, using cache when available.
        /// </summary>
        /// <param name="executablePath">Path to the browser executable</param>
        /// <returns>ImageSource for the icon, or null if extraction fails</returns>
        public static ImageSource? GetBrowserIcon(string? executablePath)
        {
            if (string.IsNullOrEmpty(executablePath))
                return null;

            lock (_cacheLock)
            {
                if (_iconCache.TryGetValue(executablePath, out var cached))
                    return cached;
            }

            try
            {
                if (!File.Exists(executablePath))
                    return null;

                using (var icon = Icon.ExtractAssociatedIcon(executablePath))
                {
                    if (icon == null)
                        return null;

                    var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    bitmapSource.Freeze(); // Thread safety

                    lock (_cacheLock)
                    {
                        _iconCache[executablePath] = bitmapSource;
                    }

                    return bitmapSource;
                }
            }
            catch (Exception)
            {
                // Log error if needed, return null for fallback
                return null;
            }
        }

        /// <summary>
        /// Clears the icon cache (useful if browser installations change)
        /// </summary>
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _iconCache.Clear();
            }
        }
    }
}
