using OpenQA.Selenium;

namespace TestingInCSharpFramework.DriverFactory;

public interface IDriverFixture
{
    IWebDriver Driver { get; }
    string DownloadDirectory { get; }
    string TakeScreenshot();
}
