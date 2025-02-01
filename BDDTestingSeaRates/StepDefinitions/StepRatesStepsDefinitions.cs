using TestingInCSharpFramework.Config;
using TestingInCSharpFramework.Utils;

namespace BDDTestingSeaRates.StepDefinitions;

[Binding]
public class StepRatesStepsDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ILandingPage _landingPage;
    private readonly IWebDriverActions _driver;
    private readonly ITestOutputHelper _output;
    private readonly TestSettings _testSettings;
    private readonly ILoginPage _loginPage;

    public StepRatesStepsDefinitions(ScenarioContext scenarioContext, ILandingPage landingPage,
        IWebDriverActions driver, ITestOutputHelper output, TestSettings testSettings,
        ILoginPage loginPage)
    {
        _scenarioContext = scenarioContext;
        _landingPage = landingPage;
        _driver = driver;
        _output = output;
        _testSettings = testSettings;
        _loginPage = loginPage;
    }

    [Given("I am on the SeaRates landing page")]
    public void GivenIAmOnTheSeaRatesLandingPage()
    {
        _driver.WaitUntilElementWithTextAppears("Find the best Freight Quote");
    }

    [Given("I accept the cookies policy")]
    public void GivenIAcceptTheCookiesPolicy()
    {
        _landingPage.AcceptAllCookies();
    }

    [Given("I successfully Login to the app")]
    public void GivenISuccessfullyLoginToTheApp()
    {
        _landingPage.NavigateToLoginPage();
        _driver.WaitUntilElementWithTextAppears("Welcome!");
        _loginPage.Login(_testSettings.UserName!, _testSettings.Password!);
    }

    [When("I enter {string} as the origin city")]
    public void WhenIEnterAsTheOriginCity(string shippingSource)
    {
        _landingPage.FillFromInputField(shippingSource);
        //_landingPage.ClickSuggestedCity(shippingSource);
        //_landingPage.ClickSuggestedCity(shippingSource);
    }

    [When("I enter {string} as the destination city")]
    public void WhenIEnterAsTheDestinationCity(string shippingDestination)
    {
        _landingPage.FillToInputField(shippingDestination);
        //_landingPage.ClickSuggestedCity(shippingDestination);
        //_landingPage.ClickSuggestedCity(shippingDestination);
    }

    [When("I select random shipping date")]
    public void WhenISelectRandomShippingDate()
    {
        _landingPage.SelectDate(FakeDataGenerator.GenerateFutureDateString());
    }
}
