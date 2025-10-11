using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BrowserSelector
{
    public class UrlRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Pattern { get; set; } = string.Empty;
        public string BrowserName { get; set; } = string.Empty;
        public string BrowserPath { get; set; } = string.Empty;
        public string BrowserType { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;
        public string ProfilePath { get; set; } = string.Empty;
        public string ProfileArguments { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
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
            var existing = rules.FirstOrDefault(r => r.Id == rule.Id);
            if (existing != null)
            {
                rules.Remove(existing);
                rules.Add(rule);
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
            var rules = LoadRules();

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
    }
}