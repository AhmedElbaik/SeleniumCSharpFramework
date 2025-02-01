using System.Globalization;

namespace BDDTestingSeaRates.Pages;

public class LandingPage : ILandingPage
{
    private readonly IWebDriverActions _driver;
    private ITestOutputHelper _outputHelper;

    public LandingPage(IWebDriverActions driver, ITestOutputHelper outputHelper)
    {
        _driver = driver;
        _outputHelper = outputHelper;
    }

    private const string MAINFILTER_ROOT_ID = "//*[@id=\"main-filter\"]";
    private const string USERCENTRICS_ROOT_ID = "//*[@id=\"usercentrics-root\"]";
    private const string DATE_BUTTON_SELECTOR = "button.zheEBo";
    private const string CALENDAR_DAYS_SELECTOR = "div[role='gridcell']";
    private const string MONTH_YEAR_CONTAINER = "div[data-testid='month-year-container']";
    private const string MONTH_SELECTOR = "button.Calendar__monthText";
    private const string YEAR_SELECTOR = "button.Calendar__yearText";
    private const string MONTH_LIST_ITEM = "button.Calendar__monthSelectorItemText";
    private const string YEAR_LIST_ITEM = "button.Calendar__yearSelectorText";

    private IWebElement SignInButton => _driver.FindElement(By.XPath("//*[@href=\"/auth/sign-in\"]"));
    private IWebElement OriginCityInput => _driver.FindElementInShadowRoot(By.XPath(MAINFILTER_ROOT_ID), By.CssSelector("#from"));
    private IWebElement DestinationCityInput => _driver.FindElementInShadowRoot(By.XPath(MAINFILTER_ROOT_ID), By.CssSelector("#to"));

    private IWebElement AcceptAllButton =>
        _driver.FindElementInShadowRoot(
            USERCENTRICS_ROOT_ID,
            "button[data-testid=\"uc-accept-all-button\"]");

    private IWebElement DenyAllButton =>
        _driver.FindElementInShadowRoot(
            USERCENTRICS_ROOT_ID,
            "button[data-testid=\"uc-deny-all-button\"]");

    // City suggestion locators using exact text
    private IWebElement CairoSuggestion =>
        _driver.FindElementInShadowRoot(
            MAINFILTER_ROOT_ID,
            "//span[text()='Cairo, EG']");

    private IWebElement IstanbulSuggestion =>
        _driver.FindElementInShadowRoot(
            MAINFILTER_ROOT_ID,
            "//span[text()='Istanbul, TR']");

    // Generic method to find any city suggestion by exact text
    private IWebElement GetCitySuggestion(string cityText) =>
        _driver.FindElementInShadowRoot(
            MAINFILTER_ROOT_ID,
            $"//span[text()='{cityText}']");


    public void NavigateToLoginPage()
    {
        _driver.ClickElement(SignInButton);
    }

    public void AcceptAllCookies()
    {
        _driver.ClickElement(AcceptAllButton);
    }

    public void DenyAllCookies()
    {
        _driver.ClickElement(DenyAllButton);
    }

    public void FillFromInputField(string text)
    {
        _driver.SendKeysToElement(OriginCityInput, text);
    }

    public void FillToInputField(string text)
    {
        _driver.SendKeysToElement(DestinationCityInput, text);
    }

    public void ClickSuggestedCity(string city)
    {
        _driver.ClickElement(GetCitySuggestion(city));
    }

    public void SelectDate(string date)
    {
        // Parse the input date
        DateTime targetDate = DateTime.ParseExact(date, "MM/dd/yyyy", CultureInfo.InvariantCulture);

        // Click the date input to open calendar
        var dateButton = _driver.FindElementInShadowRoot(By.XPath(MAINFILTER_ROOT_ID), By.CssSelector(DATE_BUTTON_SELECTOR));
        _driver.ClickElement(dateButton);

        // Select year
        var yearButton = _driver.FindElementInShadowRoot(By.XPath(MAINFILTER_ROOT_ID), By.CssSelector(YEAR_SELECTOR));
        _driver.ClickElement(yearButton);

        var yearOption = _driver.FindElementInShadowRoot(
            By.XPath(MAINFILTER_ROOT_ID),
            By.XPath($"//button[contains(@class, 'Calendar__yearSelectorText') and text()='{targetDate.Year}']"));
        _driver.ClickElement(yearOption);

        // Select month
        var monthButton = _driver.FindElementInShadowRoot(By.XPath(MAINFILTER_ROOT_ID), By.CssSelector(MONTH_SELECTOR));
        _driver.ClickElement(monthButton);

        var monthOption = _driver.FindElementInShadowRoot(
            By.XPath(MAINFILTER_ROOT_ID),
            By.XPath($"//button[contains(@class, 'Calendar__monthSelectorItemText') and text()='{targetDate.ToString("MMMM")}']"));
        _driver.ClickElement(monthOption);

        // Select day
        var dayOption = _driver.FindElementInShadowRoot(
            By.XPath(MAINFILTER_ROOT_ID),
            By.XPath($"//div[contains(@class, 'Calendar__day') and not(contains(@class, '-blank')) and text()='{targetDate.Day}']"));
        _driver.ClickElement(dayOption);
    }

    public bool IsDateSelected(string expectedDate)
    {
        var dateButton = _driver.FindElementInShadowRoot(By.XPath(MAINFILTER_ROOT_ID), By.CssSelector(DATE_BUTTON_SELECTOR));
        var selectedDate = dateButton.GetAttribute("title");

        DateTime expected = DateTime.ParseExact(expectedDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);
        string expectedFormat = expected.ToString("dd MMM, yyyy");

        return selectedDate.Equals(expectedFormat, StringComparison.OrdinalIgnoreCase);
    }
}
