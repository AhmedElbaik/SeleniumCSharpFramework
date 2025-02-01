using AventStack.ExtentReports;
using AventStack.ExtentReports.Gherkin.Model;
using AventStack.ExtentReports.Reporter;
using Reqnroll.Bindings;
using System.Reflection;
using System.Text.RegularExpressions;
using TestingInCSharpFramework.Config;

namespace BDDTestingSeaRates.Hooks;

[Binding]
public class Initialization
{
    private static ExtentReports? _extentReports;
    private readonly ScenarioContext? _scenarioContext;
    private readonly FeatureContext? _featureContext;
    private readonly IDriverFixture? _driverFixture;
    private ExtentTest? _scenario;
    private readonly TestSettings _testSettings;

    public Initialization(ScenarioContext? scenarioContext, FeatureContext? featureContext, IDriverFixture? driverFixture, TestSettings testSettings)
    {
        _testSettings = testSettings;

        if (_testSettings.IsHeadlessBrowser())
        {
            return;
        }

        _scenarioContext = scenarioContext ?? throw new ArgumentNullException(nameof(scenarioContext));
        _featureContext = featureContext ?? throw new ArgumentNullException(nameof(featureContext));
        _driverFixture = driverFixture ?? throw new ArgumentNullException(nameof(driverFixture));
    }

    [BeforeTestRun(Order = 1)]
    public static void InitializeExtentReports()
    {
        var extentReport = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/extentreport.html";
        var spark = new ExtentSparkReporter(extentReport);
        _extentReports = new ExtentReports();
        _extentReports.AttachReporter(spark);
    }

    [BeforeScenario(Order = 1)]
    public void BeforeScenario()
    {
        if (_testSettings.IsHeadlessBrowser())
        {
            return;
        }

        if (_scenarioContext == null)
        {
            throw new InvalidOperationException("ScenarioContext is null.");
        }

        var feature = _extentReports!.CreateTest<Feature>(_scenarioContext.ScenarioInfo.Title);
        var scenarioTags = _scenarioContext.ScenarioInfo.Tags;
        var testSuiteTag = scenarioTags.FirstOrDefault(tag => tag.EndsWith("_Page"));
        var groupName = testSuiteTag?.Replace("_Page", "");
        _scenario = feature.CreateNode<Scenario>(_scenarioContext.ScenarioInfo.Title).AssignCategory(groupName);
    }

    [AfterStep]
    public void AfterStep()
    {
        if (_testSettings.IsHeadlessBrowser() || _scenarioContext == null)
        {
            Console.WriteLine("Skipping AfterStep actions due to headless mode or null scenario context.");
            return;
        }

        var fileName = $"{_scenarioContext.ScenarioInfo.Title.Trim()}_{Regex.Replace(_scenarioContext.ScenarioInfo.Title, @"\s", "")}";
        var screenshotPath = _driverFixture?.TakeScreenshot();

        if (string.IsNullOrEmpty(screenshotPath))
        {
            Console.WriteLine("Screenshot capture failed.");
        }

        if (_scenarioContext.TestError == null)
        {
            switch (_scenarioContext.StepContext.StepInfo.StepDefinitionType)
            {
                case StepDefinitionType.Given:
                    _scenario!.CreateNode<Given>(_scenarioContext.StepContext.StepInfo.Text);
                    break;
                case StepDefinitionType.When:
                    _scenario!.CreateNode<When>(_scenarioContext.StepContext.StepInfo.Text);
                    break;
                case StepDefinitionType.Then:
                    _scenario!.CreateNode<Then>(_scenarioContext.StepContext.StepInfo.Text)
                        .Pass("A screenshot for a passed Then step",
                        MediaEntityBuilder.CreateScreenCaptureFromBase64String(screenshotPath).Build());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
        {
            switch (_scenarioContext.StepContext.StepInfo.StepDefinitionType)
            {
                case StepDefinitionType.Given:
                    _scenario!
                        .CreateNode<Given>(_scenarioContext.StepContext.StepInfo.Text)
                        .Fail(_scenarioContext.TestError.Message,
                        MediaEntityBuilder.CreateScreenCaptureFromBase64String(screenshotPath).Build());
                    break;
                case StepDefinitionType.When:
                    _scenario!
                        .CreateNode<When>(_scenarioContext.StepContext.StepInfo.Text)
                        .Fail(_scenarioContext.TestError.Message,
                        MediaEntityBuilder.CreateScreenCaptureFromBase64String(screenshotPath).Build());
                    break;
                case StepDefinitionType.Then:
                    _scenario!
                        .CreateNode<Then>(_scenarioContext.StepContext.StepInfo.Text)
                        .Fail(_scenarioContext.TestError.Message,
                        MediaEntityBuilder.CreateScreenCaptureFromBase64String(screenshotPath).Build());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [AfterTestRun]
    public static void TearDownReport() => _extentReports!.Flush();
}
