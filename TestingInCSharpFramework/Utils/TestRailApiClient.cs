using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

public class TestRailApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public TestRailApiClient(string baseUrl, string username, string password)
    {
        _baseUrl = baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/";

        _httpClient = new HttpClient();
        var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<int> CreateTestRun(string name, string description, int projectId, int suiteId, List<int> sectionIds)
    {
        try
        {
            // First, get all test cases from specified sections by section ID
            var casesInSections = new HashSet<int>();
            foreach (var sectionId in sectionIds)
            {
                var endpoint = $"index.php?/api/v2/get_cases/{projectId}&suite_id={suiteId}&section_id={sectionId}";
                var response = await SendGetAsync(endpoint);
                var casesResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                if (casesResponse != null && casesResponse.TryGetValue("cases", out var casesObject))
                {
                    // Cast casesObject to JArray
                    var cases = casesObject as JArray;
                    if (cases != null)
                    {
                        foreach (JObject testCase in cases)
                        {
                            casesInSections.Add(Convert.ToInt32(testCase["id"]));
                        }
                    }
                }
            }

            // Create test run with specific cases
            var createRunEndpoint = $"index.php?/api/v2/add_run/{projectId}";
            var data = new Dictionary<string, object>
        {
            { "suite_id", suiteId },
            { "name", name },
            { "description", description },
            { "include_all", false },
            { "case_ids", casesInSections.ToList() }
        };

            var runResponse = await SendPostAsync(createRunEndpoint, data);
            var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(runResponse);
            return Convert.ToInt32(result!["id"]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating test run: {ex.Message}");
            throw;
        }
    }

    public async Task AddResultForCase(int runId, int caseId, int statusId, string comment, string version, string testEnvironment, string machineInfo)
    {
        var endpoint = $"index.php?/api/v2/add_result_for_case/{runId}/{caseId}";
        var data = new Dictionary<string, object>
        {
            { "status_id", statusId },
            { "comment", $"{comment}\n\nEnvironment Information:\n{machineInfo}" },
            { "version", version },
            { "custom_test_environment", testEnvironment }
        };

        await SendPostAsync(endpoint, data);
    }

    public async Task<IEnumerable<TestRailResult>> GetResultsForCase(int runId, int caseId)
    {
        var endpoint = $"index.php?/api/v2/get_results_for_case/{runId}/{caseId}";
        var uri = new Uri(new Uri(_baseUrl), endpoint);

        var response = await _httpClient.GetAsync(uri);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonConvert.DeserializeObject<IEnumerable<TestRailResult>>(responseContent)!;
    }

    private async Task<string> SendPostAsync(string endpoint, Dictionary<string, object> data)
    {
        var uri = new Uri(new Uri(_baseUrl), endpoint);
        var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(uri, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Request URI: {uri}");
        Console.WriteLine($"Request Content: {JsonConvert.SerializeObject(data)}");
        Console.WriteLine($"Response Status Code: {response.StatusCode}");
        Console.WriteLine($"Response Content: {responseContent}");

        response.EnsureSuccessStatusCode();
        return responseContent;
    }

    private async Task<string> SendGetAsync(string endpoint)
    {
        var uri = new Uri(new Uri(_baseUrl), endpoint);
        var response = await _httpClient.GetAsync(uri);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return responseContent;
    }

    public async Task<int> GetProjectId(string projectName)
    {
        var endpoint = "index.php?/api/v2/get_projects";
        var response = await SendGetAsync(endpoint);
        var projects = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);

        var project = projects!.FirstOrDefault(p => p["name"].ToString() == projectName);
        if (project == null)
            throw new Exception($"Project '{projectName}' not found.");

        return Convert.ToInt32(project["id"]);
    }

    public async Task<int> GetSuiteId(int projectId, string suiteName)
    {
        var endpoint = $"index.php?/api/v2/get_suites/{projectId}";
        var response = await SendGetAsync(endpoint);
        var suites = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);

        var suite = suites!.FirstOrDefault(s => s["name"].ToString() == suiteName);
        if (suite == null)
            throw new Exception($"Suite '{suiteName}' not found in project {projectId}.");

        return Convert.ToInt32(suite["id"]);
    }

    public async Task<string> GetResultsForCaseRaw(int runId, int caseId)
    {
        var endpoint = $"index.php?/api/v2/get_results_for_case/{runId}/{caseId}";
        var uri = new Uri(new Uri(_baseUrl), endpoint);

        var response = await _httpClient.GetAsync(uri);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return responseContent;
    }
}

public class TestRailResult
{
    [JsonProperty("created_on")]
    public string? CreatedOn { get; set; }

    [JsonProperty("status_id")]
    public int StatusId { get; set; }

    [JsonProperty("comment")]
    public string? Comment { get; set; }
}
