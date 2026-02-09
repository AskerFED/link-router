using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BrowserSelector.Services
{
    /// <summary>
    /// Service for downloading, caching, and providing profile avatar images.
    /// </summary>
    public static class ProfileAvatarService
    {
        private static readonly Dictionary<string, ImageSource> _avatarCache = new Dictionary<string, ImageSource>();
        private static readonly object _cacheLock = new object();
        private static readonly string _cacheFolder;
        private static readonly HttpClient _httpClient;

        static ProfileAvatarService()
        {
            _cacheFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BrowserSelector", "avatars");

            // Ensure cache folder exists
            if (!Directory.Exists(_cacheFolder))
            {
                Directory.CreateDirectory(_cacheFolder);
            }

            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Gets the avatar for a profile, downloading if necessary.
        /// </summary>
        /// <param name="avatarUrl">URL of the avatar image</param>
        /// <param name="profilePath">Profile path used as cache key</param>
        /// <returns>ImageSource for the avatar, or null if unavailable</returns>
        public static async Task<ImageSource?> GetAvatarAsync(string? avatarUrl, string profilePath)
        {
            if (string.IsNullOrEmpty(avatarUrl))
                return null;

            var cacheKey = GetCacheKey(profilePath);

            // Check memory cache first
            lock (_cacheLock)
            {
                if (_avatarCache.TryGetValue(cacheKey, out var cached))
                    return cached;
            }

            // Check disk cache
            var cachedImage = LoadFromDiskCache(cacheKey);
            if (cachedImage != null)
            {
                lock (_cacheLock)
                {
                    _avatarCache[cacheKey] = cachedImage;
                }
                return cachedImage;
            }

            // Download from URL
            try
            {
                var imageBytes = await _httpClient.GetByteArrayAsync(avatarUrl);
                var image = CreateBitmapImage(imageBytes);

                if (image != null)
                {
                    // Save to disk cache
                    SaveToDiskCache(cacheKey, imageBytes);

                    // Add to memory cache
                    lock (_cacheLock)
                    {
                        _avatarCache[cacheKey] = image;
                    }

                    return image;
                }
            }
            catch (Exception)
            {
                // Network error, return null for fallback
            }

            return null;
        }

        /// <summary>
        /// Gets a cached avatar synchronously (memory cache only).
        /// </summary>
        public static ImageSource? GetCachedAvatar(string profilePath)
        {
            var cacheKey = GetCacheKey(profilePath);

            lock (_cacheLock)
            {
                if (_avatarCache.TryGetValue(cacheKey, out var cached))
                    return cached;
            }

            // Try disk cache
            var cachedImage = LoadFromDiskCache(cacheKey);
            if (cachedImage != null)
            {
                lock (_cacheLock)
                {
                    _avatarCache[cacheKey] = cachedImage;
                }
                return cachedImage;
            }

            return null;
        }

        /// <summary>
        /// Pre-fetches avatars for a list of profiles in parallel.
        /// </summary>
        public static async Task PrefetchAvatarsAsync(IEnumerable<ProfileInfo> profiles)
        {
            var tasks = new List<Task>();

            foreach (var profile in profiles)
            {
                if (!string.IsNullOrEmpty(profile.AvatarUrl))
                {
                    tasks.Add(GetAvatarAsync(profile.AvatarUrl, profile.Path));
                }
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Loads all avatars for all browser profiles at app startup.
        /// Call this early in the app lifecycle for best UX.
        /// </summary>
        public static void LoadAllAvatarsAtStartup()
        {
            // Fire and forget - load in background
            Task.Run(async () =>
            {
                try
                {
                    var allProfiles = new List<ProfileInfo>();

                    // Get all installed browsers
                    var browsers = BrowserDetector.GetInstalledBrowsers();

                    foreach (var browser in browsers)
                    {
                        var profiles = BrowserDetector.GetBrowserProfiles(browser);
                        allProfiles.AddRange(profiles);
                    }

                    // Pre-fetch all avatars
                    await PrefetchAvatarsAsync(allProfiles);
                }
                catch
                {
                    // Silently ignore startup loading errors
                }
            });
        }

        /// <summary>
        /// Checks if an avatar is already cached for a profile path.
        /// </summary>
        public static bool IsAvatarCached(string profilePath)
        {
            var cacheKey = GetCacheKey(profilePath);

            lock (_cacheLock)
            {
                if (_avatarCache.ContainsKey(cacheKey))
                    return true;
            }

            // Also check disk cache
            var cachePath = GetDiskCachePath(cacheKey);
            return File.Exists(cachePath);
        }

        /// <summary>
        /// Clears all cached avatars.
        /// </summary>
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _avatarCache.Clear();
            }

            // Optionally clear disk cache
            try
            {
                if (Directory.Exists(_cacheFolder))
                {
                    foreach (var file in Directory.GetFiles(_cacheFolder, "*.png"))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        private static string GetCacheKey(string profilePath)
        {
            // Create a hash of the profile path for the cache filename
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(profilePath));
                return BitConverter.ToString(bytes).Replace("-", "").Substring(0, 16).ToLowerInvariant();
            }
        }

        private static string GetDiskCachePath(string cacheKey)
        {
            return Path.Combine(_cacheFolder, $"{cacheKey}.png");
        }

        private static ImageSource? LoadFromDiskCache(string cacheKey)
        {
            var cachePath = GetDiskCachePath(cacheKey);

            if (!File.Exists(cachePath))
                return null;

            try
            {
                // Check if cache is still valid (7 days)
                var fileInfo = new FileInfo(cachePath);
                if (DateTime.Now - fileInfo.LastWriteTime > TimeSpan.FromDays(7))
                {
                    File.Delete(cachePath);
                    return null;
                }

                var bytes = File.ReadAllBytes(cachePath);
                return CreateBitmapImage(bytes);
            }
            catch
            {
                return null;
            }
        }

        private static void SaveToDiskCache(string cacheKey, byte[] imageBytes)
        {
            try
            {
                var cachePath = GetDiskCachePath(cacheKey);
                File.WriteAllBytes(cachePath, imageBytes);
            }
            catch
            {
                // Ignore save errors
            }
        }

        private static BitmapImage? CreateBitmapImage(byte[] imageBytes)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(imageBytes);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 64; // Limit size for performance
                bitmap.EndInit();
                bitmap.Freeze(); // Thread safety
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
