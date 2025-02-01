using TestingInCSharpFramework.DriverFactory;

namespace TestingInCSharpFramework.Config;

public class TestSettings
{
    public BrowserType BrowserType { get; set; }
    public Uri? ApplicationUrl { get; set; }
    public float? TimeoutInterval { get; set; }
    public float? TimeoutPollingInterval { get; set; }
    public TestRunType TestRunType { get; set; }
    public Uri? GridUri { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? BrowserMode { get; set; }
    public string? DockerSharedFolder { get; set; }
    public string? DockerImageName { get; set; }
    public string? CtsEdr { get; set; }
    public bool IsHeadlessBrowser(bool enabled = false)
    {
        return enabled && BrowserMode == "--headless";
    }
    public Func<bool> IsDefaultBrowser => () => BrowserMode == "--default";
    public Func<bool> IsGrid => () => TestRunType == TestRunType.Grid;
}

// for Initialize epic branch for Service Book Tests- Set 2 
public enum TestRunType
{
    Local,
    Grid
}
