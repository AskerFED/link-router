using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BrowserSelector
{
    /// <summary>
    /// Test automation window for running automated tests
    /// </summary>
    public partial class TestAutomationWindow : Window
    {
        private ObservableCollection<TestResult> _testResults;
        private bool _isRunning = false;

        public TestAutomationWindow()
        {
            InitializeComponent();
            _testResults = new ObservableCollection<TestResult>();
            TestResultsControl.ItemsSource = _testResults;
            InitializeTests();
            UpdateSummary();
        }

        private void InitializeTests()
        {
            _testResults.Clear();
            _testResults.Add(new TestResult { Name = "Load installed browsers", Status = TestStatus.Pending });
            _testResults.Add(new TestResult { Name = "Create URL rule", Status = TestStatus.Pending });
            _testResults.Add(new TestResult { Name = "Edit URL rule", Status = TestStatus.Pending });
            _testResults.Add(new TestResult { Name = "Delete URL rule", Status = TestStatus.Pending });
            _testResults.Add(new TestResult { Name = "Create URL group", Status = TestStatus.Pending });
            _testResults.Add(new TestResult { Name = "URL group matching", Status = TestStatus.Pending });
            _testResults.Add(new TestResult { Name = "Delete URL group", Status = TestStatus.Pending });
            _testResults.Add(new TestResult { Name = "Priority: URL Group > Individual Rule", Status = TestStatus.Pending });
            _testResults.Add(new TestResult { Name = "Built-in groups exist", Status = TestStatus.Pending });
        }

        private async void RunTests_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning) return;

            _isRunning = true;
            RunTestsButton.Content = "Running...";
            RunTestsButton.IsEnabled = false;
            InitializeTests();

            var totalStopwatch = Stopwatch.StartNew();

            try
            {
                // Test 1: Load installed browsers
                await RunTest(0, () =>
                {
                    var browsers = BrowserDetector.GetInstalledBrowsers();
                    if (browsers == null || browsers.Count == 0)
                        throw new Exception("No browsers detected");
                    return $"Found {browsers.Count} browser(s)";
                });

                // Test 2: Create URL rule
                string testRuleId = string.Empty;
                await RunTest(1, () =>
                {
                    var rule = new UrlRule
                    {
                        Pattern = "test-automation-rule.example.com",
                        BrowserName = "Test Browser",
                        BrowserPath = "C:\\test.exe",
                        ProfileName = "Test Profile"
                    };
                    UrlRuleManager.AddRule(rule);
                    testRuleId = rule.Id;

                    var savedRules = UrlRuleManager.LoadRules();
                    if (!savedRules.Any(r => r.Id == testRuleId))
                        throw new Exception("Rule not found after save");

                    return "Rule created and saved";
                });

                // Test 3: Edit URL rule
                await RunTest(2, () =>
                {
                    var rules = UrlRuleManager.LoadRules();
                    var rule = rules.FirstOrDefault(r => r.Id == testRuleId);
                    if (rule == null)
                        throw new Exception("Rule not found for edit");

                    rule.Pattern = "test-automation-rule-edited.example.com";
                    UrlRuleManager.UpdateRule(rule);

                    var updatedRules = UrlRuleManager.LoadRules();
                    var updated = updatedRules.FirstOrDefault(r => r.Id == testRuleId);
                    if (updated?.Pattern != "test-automation-rule-edited.example.com")
                        throw new Exception("Rule edit not persisted");

                    return "Rule edited successfully";
                });

                // Test 4: Delete URL rule
                await RunTest(3, () =>
                {
                    UrlRuleManager.DeleteRule(testRuleId);

                    var rules = UrlRuleManager.LoadRules();
                    if (rules.Any(r => r.Id == testRuleId))
                        throw new Exception("Rule still exists after delete");

                    return "Rule deleted successfully";
                });

                // Test 5: Create URL group
                string testGroupId = string.Empty;
                await RunTest(4, () =>
                {
                    var group = new UrlGroup
                    {
                        Name = "Test Automation Group",
                        Description = "Created by test automation",
                        IsEnabled = true,
                        UrlPatterns = new List<string> { "test-auto-1.com", "test-auto-2.com" },
                        Behavior = UrlGroupBehavior.UseDefault
                    };
                    UrlGroupManager.AddGroup(group);
                    testGroupId = group.Id;

                    var savedGroups = UrlGroupManager.LoadGroups();
                    if (!savedGroups.Any(g => g.Id == testGroupId))
                        throw new Exception("Group not found after save");

                    return "Group created with 2 patterns";
                });

                // Test 6: URL group matching
                await RunTest(5, () =>
                {
                    // Clear cache to ensure fresh load
                    UrlGroupManager.ClearGroupsCache();

                    var (matchedGroup, _) = UrlGroupManager.FindMatchingGroup("https://test-auto-1.com/page");
                    if (matchedGroup == null || matchedGroup.Id != testGroupId)
                        throw new Exception("URL matching failed");

                    return "URL matched to correct group";
                });

                // Test 7: Delete URL group
                await RunTest(6, () =>
                {
                    UrlGroupManager.DeleteGroup(testGroupId);

                    var groups = UrlGroupManager.LoadGroups();
                    if (groups.Any(g => g.Id == testGroupId))
                        throw new Exception("Group still exists after delete");

                    return "Group deleted successfully";
                });

                // Test 8: Priority system - URL Group > Individual Rule
                await RunTest(7, () =>
                {
                    // Create test data for priority testing
                    var urlGroup = new UrlGroup
                    {
                        Name = "Priority Test URL Group",
                        IsEnabled = true,
                        UrlPatterns = new List<string> { "priority-test.example.com" },
                        Behavior = UrlGroupBehavior.UseDefault,
                        DefaultBrowserName = "URLGroupBrowser"
                    };
                    UrlGroupManager.AddGroup(urlGroup);

                    var rule = new UrlRule
                    {
                        Pattern = "priority-test.example.com",
                        BrowserName = "RuleBrowser"
                    };
                    UrlRuleManager.AddRule(rule);

                    // Clear caches
                    UrlGroupManager.ClearGroupsCache();
                    UrlRuleManager.ClearCache();

                    // Test priority: URL Group should win
                    var match = UrlRuleManager.FindMatch("https://priority-test.example.com/test");

                    // Cleanup
                    UrlGroupManager.DeleteGroup(urlGroup.Id);
                    UrlRuleManager.DeleteRule(rule.Id);

                    if (match.Type != MatchType.UrlGroup)
                        throw new Exception($"Expected UrlGroup match, got {match.Type}");

                    return "URL Group has highest priority (correct)";
                });

                // Test 9: Built-in groups exist
                await RunTest(8, () =>
                {
                    UrlGroupManager.EnsureBuiltInGroupsExist();
                    var groups = UrlGroupManager.LoadGroups();

                    var m365 = groups.FirstOrDefault(g => g.Id == "builtin-m365");
                    var google = groups.FirstOrDefault(g => g.Id == "builtin-google");

                    if (m365 == null || google == null)
                        throw new Exception("Built-in groups not found");

                    return $"M365 ({m365.PatternCount} URLs), Google ({google.PatternCount} URLs)";
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"Test automation error: {ex.Message}");
            }

            totalStopwatch.Stop();
            TotalTime.Text = $"Total: {totalStopwatch.ElapsedMilliseconds}ms";

            _isRunning = false;
            RunTestsButton.Content = "Run All Tests";
            RunTestsButton.IsEnabled = true;
            UpdateSummary();
        }

        private async Task RunTest(int index, Func<string> testAction)
        {
            if (index < 0 || index >= _testResults.Count) return;

            var result = _testResults[index];
            result.Status = TestStatus.Running;
            result.Message = "Running...";
            RefreshList();

            var stopwatch = Stopwatch.StartNew();

            await Task.Run(() =>
            {
                try
                {
                    string message = testAction();
                    stopwatch.Stop();

                    Dispatcher.Invoke(() =>
                    {
                        result.Status = TestStatus.Passed;
                        result.Message = message;
                        result.Duration = stopwatch.ElapsedMilliseconds;
                        RefreshList();
                    });
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();

                    Dispatcher.Invoke(() =>
                    {
                        result.Status = TestStatus.Failed;
                        result.Message = ex.Message;
                        result.Duration = stopwatch.ElapsedMilliseconds;
                        RefreshList();
                    });
                }
            });

            // Small delay for UI visibility
            await Task.Delay(100);
        }

        private void RefreshList()
        {
            TestResultsControl.ItemsSource = null;
            TestResultsControl.ItemsSource = _testResults;
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            int passed = _testResults.Count(t => t.Status == TestStatus.Passed);
            int failed = _testResults.Count(t => t.Status == TestStatus.Failed);
            int total = _testResults.Count;

            SummaryText.Text = $"{passed + failed}/{total} tests completed";
            PassedCount.Text = $"Passed: {passed}";
            FailedCount.Text = $"Failed: {failed}";

            if (passed + failed == total && total > 0)
            {
                SummaryText.Foreground = failed == 0
                    ? System.Windows.Media.Brushes.Green
                    : System.Windows.Media.Brushes.Red;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    #region Test Result Model

    public enum TestStatus
    {
        Pending,
        Running,
        Passed,
        Failed
    }

    public class TestResult : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private TestStatus _status;
        private string _message = string.Empty;
        private long _duration;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public TestStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(nameof(Message)); OnPropertyChanged(nameof(HasMessage)); }
        }

        public long Duration
        {
            get => _duration;
            set { _duration = value; OnPropertyChanged(nameof(Duration)); OnPropertyChanged(nameof(HasDuration)); }
        }

        public bool HasMessage => !string.IsNullOrEmpty(Message);
        public bool HasDuration => Duration > 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #endregion
}
