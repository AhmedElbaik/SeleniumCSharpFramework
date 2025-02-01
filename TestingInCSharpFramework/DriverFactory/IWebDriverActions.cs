using OpenQA.Selenium;
using System.Collections.ObjectModel;

namespace TestingInCSharpFramework.DriverFactory;

public interface IWebDriverActions
{
    IWebElement FindElement(By elementLocator);
    IEnumerable<IWebElement> FindElements(By elementLocator);
    IWebDriver Driver { get; }
    void ClickElement(IWebElement element, int maxRetries = 5, int delayBetweenRetriesMs = 3000);
    void SendKeysToElement(IWebElement element, string keys, int maxRetries = 5, int delayBetweenRetriesMs = 3000);
    void WaitUntilElementNotDisplayed(By locator, int timeoutInSeconds = 20);
    string GetElementText(IWebElement element, int maxRetries = 5, int delayBetweenRetriesMs = 3000);
    bool IsElementDisplayed(IWebElement element, int maxRetries = 5, int delayBetweenRetriesMs = 3000);
    IWebElement WaitUntilElementWithTextAppears(string text, int timeoutInSeconds = 30);
    IWebElement FindElement(ISearchContext ctx, By by);
    ReadOnlyCollection<IWebElement> FindElements(ISearchContext ctx, By by);
    IWebElement FindElementCaseInsensitive(IWebDriver driver, string searchValue);
    public IReadOnlyCollection<IWebElement> FindElementsContainingText(IWebDriver driver, string searchValue);
    void RetryAction(Func<bool> action, int retries);
    void SelectFromDropdown(string dataQaid, string ariaLabelledBy, string itemText);
    void WaitForPageToLoadCorrectly();
    IWebElement GetTableCell(int rowNumber, int columnNumber);
    IWebElement GetTableCell(int rowNumber, int columnNumber, string tableHeader);
    IWebElement FindElementInShadowRoot(By shadowHostLocator, By elementLocator);
    IEnumerable<IWebElement> FindElementsInShadowRoot(By shadowHostLocator, By elementLocator);
    IWebElement FindElementInShadowRoot(string shadowHostXPath, string elementCssSelector);
    IEnumerable<IWebElement> FindElementsInShadowRoot(string shadowHostXPath, string elementCssSelector);
}