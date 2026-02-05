using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text.Json;
using Microsoft.Win32;

namespace BrowserSelector
{
    public class BrowserInfo
    {
        public string Name { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class ProfileInfo
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Path { get; set; }
        public string Arguments { get; set; }

        public string AvatarUrl { get; set; }   // NEW
        public int ProfileIndex { get; set; }   // NEW (fallback)

        public string DisplayName =>
            string.IsNullOrWhiteSpace(Email)
                ? Name
                : $"{Name} ({Email})";
    }


    public static class BrowserDetector
    {
        public static List<BrowserInfo> GetInstalledBrowsers()
        {
            var browsers = new List<BrowserInfo>();

            // Check for Chrome
            var chromePath = GetChromePath();
            if (!string.IsNullOrEmpty(chromePath))
            {
                browsers.Add(new BrowserInfo
                {
                    Name = "Google Chrome",
                    ExecutablePath = chromePath,
                    Type = "Chrome"
                });
            }

            // Check for Edge
            var edgePath = GetEdgePath();
            if (!string.IsNullOrEmpty(edgePath))
            {
                browsers.Add(new BrowserInfo
                {
                    Name = "Microsoft Edge",
                    ExecutablePath = edgePath,
                    Type = "Edge"
                });
            }

            // Check for Firefox
            var firefoxPath = GetFirefoxPath();
            if (!string.IsNullOrEmpty(firefoxPath))
            {
                browsers.Add(new BrowserInfo
                {
                    Name = "Mozilla Firefox",
                    ExecutablePath = firefoxPath,
                    Type = "Firefox"
                });
            }

            // Check for Brave
            var bravePath = GetBravePath();
            if (!string.IsNullOrEmpty(bravePath))
            {
                browsers.Add(new BrowserInfo
                {
                    Name = "Brave Browser",
                    ExecutablePath = bravePath,
                    Type = "Chrome"
                });
            }

            // Check for Opera
            var operaPath = GetOperaPath();
            if (!string.IsNullOrEmpty(operaPath))
            {
                browsers.Add(new BrowserInfo
                {
                    Name = "Opera",
                    ExecutablePath = operaPath,
                    Type = "Chrome"
                });
            }

            // Check for Opera GX
            var operaGXPath = GetOperaGXPath();
            if (!string.IsNullOrEmpty(operaGXPath))
            {
                browsers.Add(new BrowserInfo
                {
                    Name = "Opera GX",
                    ExecutablePath = operaGXPath,
                    Type = "Chrome"
                });
            }

            return browsers;
        }

        public static List<ProfileInfo> GetBrowserProfiles(BrowserInfo browser)
        {
            var profiles = new List<ProfileInfo>();

            switch (browser.Type)
            {
                case "Chrome":
                    profiles = GetChromeProfiles(browser.Name);
                    break;
                case "Edge":
                    profiles = GetEdgeProfiles();
                    break;
                case "Firefox":
                    profiles = GetFirefoxProfiles();
                    break;
            }

            // Always add a default profile option
            if (profiles.Count == 0)
            {
                profiles.Add(new ProfileInfo
                {
                    Name = "Default",
                    Path = "Default",
                    Arguments = string.Empty
                });
            }

            return profiles;
        }

        private static string? GetChromePath()
        {
            var paths = new[]
            {
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
            };

            return paths.FirstOrDefault(File.Exists);
        }

        private static string? GetEdgePath()
        {
            var paths = new[]
            {
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"
            };

            return paths.FirstOrDefault(File.Exists);
        }

        private static string? GetFirefoxPath()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\firefox.exe");
                return key?.GetValue("") as string;
            }
            catch
            {
                var paths = new[]
                {
                    @"C:\Program Files\Mozilla Firefox\firefox.exe",
                    @"C:\Program Files (x86)\Mozilla Firefox\firefox.exe"
                };

                return paths.FirstOrDefault(File.Exists);
            }
        }

        private static string? GetBravePath()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var paths = new[]
            {
                System.IO.Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\Application\brave.exe"),
                @"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe",
                @"C:\Program Files (x86)\BraveSoftware\Brave-Browser\Application\brave.exe"
            };

            return paths.FirstOrDefault(File.Exists);
        }

        private static string? GetOperaPath()
        {
            try
            {
                // Try registry (machine-wide)
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Clients\StartMenuInternet\OperaStable\shell\open\command"))
                {
                    var value = key?.GetValue("") as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        // The registry value can be like: "\"C:\\Users\\User\\AppData\\Local\\Programs\\Opera\\launcher.exe\" --"
                        var exePath = value.Split('"').FirstOrDefault(s => s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
                        if (File.Exists(exePath))
                            return exePath;
                    }
                }

                // Try registry (per-user)
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Clients\StartMenuInternet\OperaStable\shell\open\command"))
                {
                    var value = key?.GetValue("") as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        var exePath = value.Split('"').FirstOrDefault(s => s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
                        if (File.Exists(exePath))
                            return exePath;
                    }
                }
            }
            catch
            {
                // Ignore registry errors
            }

            // Fallback: check common file system paths
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            var paths = new[]
            {
        Path.Combine(localAppData, @"Programs\Opera\launcher.exe"),
        Path.Combine(localAppData, @"Programs\Opera\opera.exe"),
        Path.Combine(programFiles, @"Opera\launcher.exe"),
        Path.Combine(programFilesX86, @"Opera\launcher.exe")
    };

            return paths.FirstOrDefault(File.Exists);
        }

        private static string? GetOperaGXPath()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            var paths = new[]
            {
                System.IO.Path.Combine(localAppData, @"Programs\Opera GX\launcher.exe"),
                System.IO.Path.Combine(programFiles, @"Opera GX\launcher.exe"),
                System.IO.Path.Combine(programFilesX86, @"Opera GX\launcher.exe")
            };

            return paths.FirstOrDefault(File.Exists);
        }

        private static List<ProfileInfo> GetChromeProfiles(string browserName)
        {
            var profiles = new List<ProfileInfo>();
            string userDataPath;

            // Determine user data path based on browser
            if (browserName.Contains("Brave"))
            {
                userDataPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"BraveSoftware\Brave-Browser\User Data");
            }
            else if (browserName.Contains("Opera GX"))
            {
                userDataPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Opera Software\Opera GX Stable");
            }
            else if (browserName.Contains("Opera"))
            {
                userDataPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Opera Software\Opera Stable");
            }
            else
            {
                userDataPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Google\Chrome\User Data");
            }

            if (!Directory.Exists(userDataPath))
                return profiles;

            // Add Default profile
            var defaultPrefs = System.IO.Path.Combine(userDataPath, "Default", "Preferences");
            if (File.Exists(defaultPrefs))
            {
                var profileName = GetProfileNameFromPreferences(defaultPrefs) ?? "Default";
                profiles.Add(new ProfileInfo
                {
                    Name = profileName,
                    Path = System.IO.Path.Combine(userDataPath, "Default"),
                    Arguments = "--profile-directory=\"Default\""
                });
            }

            // Add numbered profiles
            var profileDirs = Directory.GetDirectories(userDataPath, "Profile *");
            foreach (var dir in profileDirs)
            {
                var prefsFile = System.IO.Path.Combine(dir, "Preferences");
                if (File.Exists(prefsFile))
                {
                    var dirName = System.IO.Path.GetFileName(dir);
                    var profileName = GetProfileNameFromPreferences(prefsFile) ?? dirName;

                    profiles.Add(new ProfileInfo
                    {
                        Name = profileName,
                        Path = dir,
                        Arguments = $"--profile-directory=\"{dirName}\""
                    });
                }
            }

            // Handle duplicate names by appending profile directory
            var duplicateNames = profiles.GroupBy(p => p.Name)
                                         .Where(g => g.Count() > 1)
                                         .Select(g => g.Key)
                                         .ToHashSet();

            foreach (var profile in profiles)
            {
                if (duplicateNames.Contains(profile.Name))
                {
                    // Extract profile directory name from Arguments
                    var match = System.Text.RegularExpressions.Regex.Match(
                        profile.Arguments,
                        @"--profile-directory=""([^""]+)"""
                    );

                    if (match.Success)
                    {
                        string dirName = match.Groups[1].Value;
                        profile.Name = $"{profile.Name} ({dirName})";
                    }
                }
            }

            return profiles;
        }


        private static List<ProfileInfo> GetEdgeProfiles()
        {
            var profiles = new List<ProfileInfo>();
            var userDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\User Data");

            if (!Directory.Exists(userDataPath))
                return profiles;

            // Add Default profile
            var defaultPrefs = System.IO.Path.Combine(userDataPath, "Default", "Preferences");
            if (File.Exists(defaultPrefs))
            {
                var profileName = GetProfileNameFromPreferences(defaultPrefs) ?? "Default";
                profiles.Add(new ProfileInfo
                {
                    Name = profileName,
                    Path = System.IO.Path.Combine(userDataPath, "Default"),
                    Arguments = "--profile-directory=\"Default\""
                });
            }

            // Add numbered profiles
            var profileDirs = Directory.GetDirectories(userDataPath, "Profile *");
            foreach (var dir in profileDirs)
            {
                var prefsFile = System.IO.Path.Combine(dir, "Preferences");
                if (File.Exists(prefsFile))
                {
                    var dirName = System.IO.Path.GetFileName(dir);
                    var profileName = GetProfileNameFromPreferences(prefsFile) ?? dirName;
                    profiles.Add(new ProfileInfo
                    {
                        Name = profileName,
                        Path = dir,
                        Arguments = $"--profile-directory=\"{dirName}\""
                    });
                }
            }

            return profiles;
        }

        private static List<ProfileInfo> GetFirefoxProfiles()
        {
            var profiles = new List<ProfileInfo>();
            var profilesPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Mozilla\Firefox\Profiles");

            if (!Directory.Exists(profilesPath))
                return profiles;

            var profileDirs = Directory.GetDirectories(profilesPath);
            foreach (var dir in profileDirs)
            {
                var dirName = System.IO.Path.GetFileName(dir);
                var profileName = dirName.Contains('.') ? dirName.Substring(dirName.IndexOf('.') + 1) : dirName;

                profiles.Add(new ProfileInfo
                {
                    Name = profileName,
                    Path = dir,
                    Arguments = $"-profile \"{dir}\""
                });
            }

            return profiles;
        }

         
        private static string GetProfileNameFromPreferences(string preferencesPath)
        {
            try
            {
                var json = File.ReadAllText(preferencesPath);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;

                string profileName = null;
                string email = null;


                // profile.name
                if (root.TryGetProperty("profile", out var profileProp) &&
                    profileProp.TryGetProperty("name", out var nameProp))
                {
                    profileName = nameProp.GetString();
                }

                // account_info[0].email
                if (root.TryGetProperty("account_info", out var accountInfo) &&
                    accountInfo.ValueKind == System.Text.Json.JsonValueKind.Array &&
                    accountInfo.GetArrayLength() > 0)
                {
                    var account = accountInfo[0];


                    if (account.TryGetProperty("email", out var emailProp))
                    {
                        email = emailProp.GetString();
                    }
                }

                // Compose display name
                if (!string.IsNullOrWhiteSpace(profileName) &&
                    !string.IsNullOrWhiteSpace(email))
                {
                    return $"{profileName} ({email})";
                }

                return profileName;
            }
            catch
            {
                return null;
            }
        }

        public static BrowserInfo MatchBrowserByPath(string path)
        {
            foreach (var browser in GetInstalledBrowsers())
            {
                if (browser.ExecutablePath == path)
                    return browser;
            }
            return null;
        }
    }
}