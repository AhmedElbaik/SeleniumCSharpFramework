//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using System.Runtime.InteropServices;
//using TestingInCSharpFramework.Config;

//namespace BDDTestingSeaRates.Hooks;

//[Binding]
//public class TestRailIntegrationHooks
//{
//    private static TestRailApiClient? _testRailClient;
//    private static int _currentRunId;

//    // TestRail Configuration
//    private const int ProjectId = 178;
//    private const int SuiteId = 1715;
//    private const int DefaultRunId = 70681; // Default RunId for debugging purposes is 70681
//    private const string Password = "";
//    private const string Url = "";
//    private const string Version = "December Release";
//    private const string TestEnvironment = "";
//    private const int MaxRetries = 5;

//    // Specify the section IDs for the folders containing your test cases
//    private static readonly List<int> TestSectionIds = new()
//    {
//        503062, // Automated Test Cases/Service Books Page
//        503063, // Automated Test Cases/Cost Books Page
//        527451  // Automated Test Cases/Scenario Books Page
//    };

//    private readonly ScenarioContext _scenarioContext;
//    private readonly IDriverFixture _driver;
//    private readonly TestSettings _testSettings;
//    private readonly bool _isHeadlessMode;

//    public TestRailIntegrationHooks(
//        ScenarioContext scenarioContext,
//        IDriverFixture driver,
//        TestSettings testSettings)
//    {
//        _scenarioContext = scenarioContext ?? throw new ArgumentNullException(nameof(scenarioContext));
//        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
//        _testSettings = testSettings ?? throw new ArgumentNullException(nameof(testSettings));
//        _isHeadlessMode = testSettings.IsHeadlessBrowser(true);
//    }

//    private string GetMachineInformation()
//    {
//        var osDescription = RuntimeInformation.OSDescription;
//        var osArchitecture = RuntimeInformation.OSArchitecture;
//        var processArchitecture = RuntimeInformation.ProcessArchitecture;
//        var machineName = Environment.MachineName;
//        var userDomainName = Environment.UserDomainName;
//        var browserInfo = _driver.GetType().Name + " (" + _testSettings.BrowserType + ")";
//        var isHeadless = _isHeadlessMode ? "Headless" : "Normal";

//        return $"OS: {osDescription}\n" +
//               $"OS Architecture: {osArchitecture}\n" +
//               $"Process Architecture: {processArchitecture}\n" +
//               $"Machine Name: {machineName}\n" +
//               $"Domain: {userDomainName}\n" +
//               $"Browser: {browserInfo}\n" +
//               $"Browser Mode: {isHeadless}";
//    }

//    [BeforeTestRun(Order = 3)]
//    public static async Task BeforeTestRun()
//    {
//        _testRailClient = new TestRailApiClient(Url, Username, Password);

//        var isJenkins = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_URL"));
//        if (isJenkins)
//        {
//            try
//            {
//                var buildVersion = Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "Unknown";
//                var branchName = Environment.GetEnvironmentVariable("BRANCH_NAME") ?? "Unknown";
//                var timestamp = DateTime.Now.ToString("MMM yyyy");

//                var runName = $"Automated Run - {timestamp} - Build {buildVersion} - {branchName}";
//                var description = $"Automated test run created from Jenkins Build #{buildVersion}\n" +
//                                $"Branch: {branchName}\n" +
//                                $"Execution Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

//                _currentRunId = await _testRailClient.CreateTestRun(
//                    runName,
//                    description,
//                    ProjectId,
//                    SuiteId,
//                    TestSectionIds);

//                Console.WriteLine($"Created new TestRail run with ID: {_currentRunId}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Failed to create TestRail run. Using default run ID. Error: {ex.Message}");
//                _currentRunId = DefaultRunId;
//            }
//        }
//        else
//        {
//            _currentRunId = DefaultRunId;
//        }

//    }

//    [AfterScenario(Order = 3)]
//    public async Task AfterScenarioAsync()
//    {
//        try
//        {
//            var caseId = GetTestCaseId(_scenarioContext);
//            var statusId = _scenarioContext.TestError != null ? 5 : 1;
//            var comment = _scenarioContext.TestError?.Message ?? "Test passed";
//            var machineInfo = GetMachineInformation();

//            for (int attempt = 0; attempt < MaxRetries; attempt++)
//            {
//                try
//                {
//                    await _testRailClient!.AddResultForCase(
//                        _currentRunId,
//                        caseId,
//                        statusId,
//                        comment,
//                        Version,
//                        TestEnvironment,
//                        machineInfo);

//                    var isRecent = await IsResultRecent(caseId);
//                    if (isRecent)
//                    {
//                        Console.WriteLine("Test result is recent, update was successful.");
//                        break;
//                    }
//                    else if (attempt == MaxRetries - 1)
//                    {
//                        Console.WriteLine("Failed to confirm recent result after maximum retries.");
//                    }
//                }
//                catch (Exception ex)
//                {
//                    if (attempt == MaxRetries - 1)
//                    {
//                        Console.WriteLine($"Error updating TestRail after {MaxRetries} attempts: {ex.Message}");
//                        Console.WriteLine(ex.StackTrace);
//                    }
//                    else
//                    {
//                        await Task.Delay(2000);
//                    }
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error processing TestRail result: {ex.Message}");
//            Console.WriteLine(ex.StackTrace);
//        }
//    }

//    private async Task<bool> IsResultRecent(int caseId)
//    {
//        try
//        {
//            var rawResponse = await GetResultsForCaseRaw(_currentRunId, caseId);

//            var jsonResponse = JToken.Parse(rawResponse);

//            var results = jsonResponse.Type == JTokenType.Array
//                ? jsonResponse.ToObject<List<TestRailResult>>()
//                : new List<TestRailResult> { jsonResponse.ToObject<TestRailResult>()! };

//            if (results != null && results.Any())
//            {
//                var latestResult = results.First();

//                // Debug: Print the raw CreatedOn value
//                Console.WriteLine($"Raw CreatedOn value: {latestResult.CreatedOn}");

//                // If the timestamp seems incorrect, you might need to adjust the conversion
//                var resultTime = latestResult.CreatedOn.HasValue
//                    ? DateTimeOffset.FromUnixTimeSeconds(latestResult.CreatedOn.Value)
//                    : DateTimeOffset.UtcNow;

//                var currentTime = DateTimeOffset.UtcNow;

//                Console.WriteLine($"Result Time (UTC): {resultTime}");
//                Console.WriteLine($"Current Time (UTC): {currentTime}");

//                var timeDifference = currentTime - resultTime;
//                Console.WriteLine($"Time Difference: {timeDifference.TotalMinutes} minutes");

//                // Allow results within the last 5 minutes
//                return timeDifference.TotalMinutes <= 5;
//            }
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error checking recent results: {ex.Message}");
//            Console.WriteLine(ex.StackTrace);
//        }
//        return false;
//    }

//    private async Task<string> GetResultsForCaseRaw(int runId, int caseId)
//    {
//        // Implement raw result retrieval from TestRail API
//        // This is a placeholder and should be implemented in your TestRailApiClient
//        return await _testRailClient!.GetResultsForCaseRaw(runId, caseId);
//    }

//    private int GetTestCaseId(ScenarioContext scenarioContext)
//    {
//        var caseIdTag = scenarioContext.ScenarioInfo.Tags
//            .FirstOrDefault(tag => tag.StartsWith("C", StringComparison.OrdinalIgnoreCase));

//        return caseIdTag != null
//            ? int.Parse(caseIdTag[1..])
//            : throw new Exception("Test Case ID not found in scenario tags.");
//    }

//    // Updated Result model to match TestRail's JSON structure
//    private class TestRailResult
//    {
//        [JsonProperty("id")]
//        public int Id { get; set; }

//        [JsonProperty("created_on")]
//        public long? CreatedOn { get; set; }

//        [JsonProperty("status_id")]
//        public int StatusId { get; set; }

//        [JsonProperty("comment")]
//        public string? Comment { get; set; }
//    }
//}