using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace BrowserSelector.Services
{
    /// <summary>
    /// Provides atomic JSON file operations with automatic backup and recovery.
    /// Prevents data corruption by writing to temp file first, then atomically replacing.
    /// Maintains rolling backups for enhanced data loss prevention.
    /// </summary>
    public static class JsonStorageService
    {
        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Maximum number of timestamped backups to retain per file.
        /// </summary>
        private const int MaxBackups = 5;

        /// <summary>
        /// Atomically saves data to a JSON file with automatic timestamped backup.
        /// Uses temp file + atomic replace for safe operation on NTFS.
        /// Maintains rolling backups (last N versions) for recovery.
        /// </summary>
        public static void Save<T>(string filePath, T data)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = filePath + ".tmp";
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var ext = Path.GetExtension(filePath);

            try
            {
                // Step 1: Serialize to temp file
                var json = JsonSerializer.Serialize(data, WriteOptions);
                File.WriteAllText(tempPath, json);

                // Step 2: Create timestamped backup before replacing (if file exists)
                if (File.Exists(filePath))
                {
                    try
                    {
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var backupPath = Path.Combine(directory!, $"{fileName}.{timestamp}.bak{ext}");
                        File.Copy(filePath, backupPath);

                        // Clean up old backups, keeping only the most recent ones
                        CleanupOldBackups(directory!, fileName, ext, MaxBackups);
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail the save operation
                        Logger.Log($"Warning: Could not create backup: {ex.Message}");
                    }
                }

                // Step 3: Atomic replace
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                File.Move(tempPath, filePath);
            }
            catch (Exception ex)
            {
                // Clean up temp file if it exists
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch { }

                Logger.Log($"Failed to save {filePath}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads data from a JSON file with automatic backup recovery.
        /// Falls back to timestamped backups if main file is corrupted.
        /// Shows user notification if all recovery attempts fail (but not on fresh install).
        /// </summary>
        public static T Load<T>(string filePath) where T : class, new()
        {
            // Check if data directory exists - if not, this is a fresh install
            var directory = Path.GetDirectoryName(filePath);
            bool directoryExists = !string.IsNullOrEmpty(directory) && Directory.Exists(directory);

            // If directory doesn't exist, it's a fresh install - just return empty data
            if (!directoryExists)
            {
                return new T();
            }

            // Try main file first
            if (TryLoadFile<T>(filePath, out var data))
                return data!;

            // Check if main file exists (might be corrupted if it exists but didn't load)
            bool mainFileExists = File.Exists(filePath);

            // Fall back to legacy .bak file
            var legacyBackupPath = filePath + ".bak";
            if (TryLoadFile<T>(legacyBackupPath, out data))
            {
                Logger.Log($"Recovered data from legacy backup: {legacyBackupPath}");
                RestoreBackupToMain(legacyBackupPath, filePath);
                return data!;
            }

            // Try timestamped backups (newest first)
            var recoveredData = TryRecoverFromTimestampedBackups<T>(filePath);
            if (recoveredData != null)
                return recoveredData;

            // Check if any backup files exist
            bool hasAnyBackups = HasAnyBackupFiles(filePath);

            // Only show warning if data was corrupted (file existed or backups exist)
            // Don't show warning on fresh install (no file, no backups)
            if (mainFileExists || hasAnyBackups)
            {
                NotifyUserOfDataLoss(filePath);
            }

            // Return new instance
            return new T();
        }

        /// <summary>
        /// Checks if any backup files exist for the given file path.
        /// </summary>
        private static bool HasAnyBackupFiles(string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                    return false;

                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var ext = Path.GetExtension(filePath);

                // Check for legacy backup
                if (File.Exists(filePath + ".bak"))
                    return true;

                // Check for timestamped backups
                var backups = Directory.GetFiles(directory, $"{fileName}.*.bak{ext}");
                return backups.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to recover data from timestamped backup files.
        /// Tries each backup in reverse chronological order.
        /// </summary>
        private static T? TryRecoverFromTimestampedBackups<T>(string filePath) where T : class
        {
            var directory = Path.GetDirectoryName(filePath);
            if (directory == null || !Directory.Exists(directory))
                return null;

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var ext = Path.GetExtension(filePath);

            try
            {
                // Find all timestamped backups, sorted newest first
                var backups = Directory.GetFiles(directory, $"{fileName}.*.bak{ext}")
                    .OrderByDescending(f => f)
                    .ToList();

                foreach (var backup in backups)
                {
                    if (TryLoadFile<T>(backup, out var data))
                    {
                        Logger.Log($"Recovered data from timestamped backup: {backup}");
                        RestoreBackupToMain(backup, filePath);
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error searching for backups: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Restores a backup file to the main file location.
        /// </summary>
        private static void RestoreBackupToMain(string backupPath, string mainPath)
        {
            try
            {
                File.Copy(backupPath, mainPath, overwrite: true);
                Logger.Log($"Restored backup to main file: {mainPath}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to restore backup to main file: {ex.Message}");
            }
        }

        /// <summary>
        /// Notifies the user that data could not be recovered.
        /// Shows a dialog explaining the situation and options.
        /// </summary>
        private static void NotifyUserOfDataLoss(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var directory = Path.GetDirectoryName(filePath);

            Logger.Log($"CRITICAL: All data files corrupt or missing for {filePath}");

            // Show dialog on UI thread if application is running
            try
            {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            $"Unable to load your data from '{fileName}'.\n\n" +
                            "All backup files appear to be corrupted or missing.\n\n" +
                            "Recovery options:\n" +
                            $"1. Check for manual backups in:\n   {directory}\n\n" +
                            "2. If you have exported data previously, use Import to restore.\n\n" +
                            "3. The application will continue with empty data.\n\n" +
                            "Your data will be preserved going forward once you add new rules.",
                            "Data Recovery Required",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });
                }
            }
            catch
            {
                // Ignore UI errors - app may be starting up
            }
        }

        /// <summary>
        /// Removes old backup files, keeping only the most recent ones.
        /// </summary>
        private static void CleanupOldBackups(string directory, string fileName, string ext, int maxKeep)
        {
            try
            {
                var backups = Directory.GetFiles(directory, $"{fileName}.*.bak{ext}")
                    .OrderByDescending(f => f)
                    .Skip(maxKeep)
                    .ToList();

                foreach (var old in backups)
                {
                    try
                    {
                        File.Delete(old);
                        Logger.Log($"Deleted old backup: {Path.GetFileName(old)}");
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error cleaning up old backups: {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts to load and deserialize a JSON file.
        /// Returns false if file doesn't exist, is empty, or fails to deserialize.
        /// </summary>
        private static bool TryLoadFile<T>(string path, out T? result) where T : class
        {
            result = null;
            try
            {
                if (!File.Exists(path))
                    return false;

                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                    return false;

                result = JsonSerializer.Deserialize<T>(json);
                return result != null;
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load {path}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a pre-migration backup of a file before schema changes.
        /// Stores backups in a dedicated 'backups' subdirectory with migration timestamp.
        /// </summary>
        public static void CreatePreMigrationBackup(string filePath, string migrationName)
        {
            try
            {
                if (!File.Exists(filePath))
                    return;

                var directory = Path.GetDirectoryName(filePath);
                var backupDir = Path.Combine(directory!, "backups");

                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var ext = Path.GetExtension(filePath);
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                var backupPath = Path.Combine(backupDir, $"{fileName}_pre-{migrationName}_{timestamp}{ext}");

                File.Copy(filePath, backupPath, overwrite: false);
                Logger.Log($"Created pre-migration backup: {Path.GetFileName(backupPath)}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Warning: Could not create pre-migration backup: {ex.Message}");
                // Continue anyway - migration may still succeed
            }
        }
    }
}
