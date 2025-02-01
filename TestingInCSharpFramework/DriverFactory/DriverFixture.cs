using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using TestingInCSharpFramework.Config;
using Xunit.Abstractions;

namespace TestingInCSharpFramework.DriverFactory;

public class DriverFixture : IDriverFixture, IDisposable
{
    private readonly TestSettings _testSettings;
    private readonly ITestOutputHelper _outputHelper;
    private readonly string _downloadDirectory;
    public string DownloadDirectory { get { return _downloadDirectory; } }

    public IWebDriver Driver { get; }

    public DriverFixture(TestSettings testSettings, ITestOutputHelper outputHelper)
    {
        _testSettings = testSettings;
        _outputHelper = outputHelper;

        _downloadDirectory = _testSettings.IsGrid()
            ? Path.Combine(_testSettings.DockerSharedFolder!)
            : CreateUniqueDownloadFolder().FullName;

        Driver = _testSettings.TestRunType == TestRunType.Local
            ? GetWebDriver()
            : GetRemoteWebDriver();

        Driver.Manage().Window.Maximize();

        if (_testSettings.BrowserType == BrowserType.Chrome && Driver is ChromeDriver chromeDriver)
        {
            // Set Chrome-specific download behavior
            chromeDriver.ExecuteCdpCommand("Page.setDownloadBehavior", new Dictionary<string, object>
        {
            { "behavior", "allow" },
            { "downloadPath", _downloadDirectory }
        });
        }

        Driver.Navigate().GoToUrl(_testSettings.ApplicationUrl);
    }

    private IWebDriver GetWebDriver()
    {
        return _testSettings.BrowserType switch
        {
            BrowserType.Chrome => InitializeChromeDriver(),
            BrowserType.Firefox => InitializeFirefoxDriver(),
            BrowserType.EdgeChromium => InitializeEdgeDriver(),
            _ => throw new NotSupportedException("The Browser is Not Supported")
        };
    }

    private IWebDriver GetRemoteWebDriver()
    {
        return _testSettings.BrowserType switch
        {
            BrowserType.Chrome => new RemoteWebDriver(_testSettings.GridUri, GetChromeOptions()),
            BrowserType.Firefox => new RemoteWebDriver(_testSettings.GridUri, GetFirefoxOptions()),
            BrowserType.EdgeChromium => new RemoteWebDriver(_testSettings.GridUri, GetEdgeOptions()),
            _ => new RemoteWebDriver(_testSettings.GridUri, GetEdgeOptions())
        };
    }

    private IWebDriver InitializeChromeDriver()
    {
        var options = GetChromeOptions();
        return new ChromeDriver(options);
    }

    private IWebDriver InitializeFirefoxDriver()
    {
        var options = GetFirefoxOptions();
        return new FirefoxDriver(options);
    }

    private IWebDriver InitializeEdgeDriver()
    {
        var options = GetEdgeOptions();
        return new EdgeDriver(options);
    }

    private ChromeOptions GetChromeOptions()
    {
        var options = new ChromeOptions();
        options.AddArgument("--incognito");
        options.AddArgument("--no-sandbox");
        options.AddArgument(_testSettings.BrowserMode); // headless - incognito
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-popup-blocking");
        options.AddArgument("--disable-notifications");
        options.AddUserProfilePreference("download.default_directory", _downloadDirectory);
        options.AddUserProfilePreference("download.prompt_for_download", false);
        options.AddUserProfilePreference("profile.default_content_setting_values.automatic_downloads", 1);
        options.UnhandledPromptBehavior = UnhandledPromptBehavior.Ignore;
        options.PageLoadStrategy = PageLoadStrategy.Eager;
        if (_testSettings.IsGrid())
        {
            options.AddAdditionalOption("se:recordVideo", true);
        }
        return options;
    }

    private FirefoxOptions GetFirefoxOptions()
    {
        var options = new FirefoxOptions();
        options.AddArgument("-private");
        options.SetPreference("browser.download.dir", _downloadDirectory);
        options.SetPreference("browser.download.folderList", 2);
        options.SetPreference("browser.helperApps.neverAsk.saveToDisk", "application/pdf,application/octet-stream"); // Added more MIME types
        options.SetPreference("browser.download.manager.showWhenStarting", false);
        options.SetPreference("dom.webnotifications.enabled", false);
        options.UnhandledPromptBehavior = UnhandledPromptBehavior.Ignore;
        options.PageLoadStrategy = PageLoadStrategy.Eager;
        if (_testSettings.IsGrid())
        {
            options.AddAdditionalOption("se:recordVideo", true);
        }
        return options;
    }

    private EdgeOptions GetEdgeOptions()
    {
        var options = new EdgeOptions();
        options.AddArgument("--inprivate");
        options.AddArgument("--no-sandbox");
        options.AddArgument(_testSettings.BrowserMode); // headless or inprivate
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-popup-blocking");
        options.AddArgument("--disable-notifications");
        options.AddUserProfilePreference("download.default_directory", _downloadDirectory);
        options.AddUserProfilePreference("download.prompt_for_download", false);
        options.AddUserProfilePreference("profile.default_content_setting_values.automatic_downloads", 1);
        options.UnhandledPromptBehavior = UnhandledPromptBehavior.Accept;
        options.PageLoadStrategy = PageLoadStrategy.Normal;
        if (_testSettings.IsGrid())
        {
            options.AddAdditionalOption("se:recordVideo", true);
        }
        return options;
    }

    public string TakeScreenshot()
    {
        if (Driver == null)
        {
            _outputHelper.WriteLine("Driver is not initialized. Cannot take screenshot.");
            return string.Empty;
        }
        try
        {
            var file = ((ITakesScreenshot)Driver)?.GetScreenshot();
            var screenshot = file?.AsBase64EncodedString;
            return screenshot!;
        }
        catch (NoSuchWindowException ex)
        {
            _outputHelper.WriteLine("NoSuchWindowException: " + ex.Message);
        }
        catch (WebDriverException ex)
        {
            _outputHelper.WriteLine("WebDriverException: " + ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _outputHelper.WriteLine("InvalidOperationException: " + ex.Message);
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine("Exception: " + ex.Message);
        }

        // Return a placeholder or empty string if an exception occurs
        return string.Empty;
    }


    public void Dispose()
    {
        if (!_testSettings.IsGrid())
        {
            DeleteUniqueDownloadFolder(_downloadDirectory);
        }

        Driver?.Dispose();
    }

    private DirectoryInfo CreateUniqueDownloadFolder()
    {
        string uniqueDirectoryName = "UniqueDirectoryName_" + Guid.NewGuid().ToString();
        string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        string newDirectoryPath = Path.Combine(downloadsPath, uniqueDirectoryName);
        return Directory.CreateDirectory(newDirectoryPath);
    }

    private void DeleteUniqueDownloadFolder(string folderPath)
    {
        try
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
            }
            else
            {
                _outputHelper.WriteLine($"Folder does not exist at path: {folderPath}");
            }
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine($"Error occurred while deleting folder: {ex.Message}");
        }
    }
}



public enum BrowserType
{
    Chrome,
    Firefox,
    Safari,
    EdgeChromium
}
