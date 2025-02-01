using Microsoft.TeamFoundation.Common;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Collections.ObjectModel;
using TestingInCSharpFramework.Config;
using Xunit.Abstractions;

namespace TestingInCSharpFramework.DriverFactory;

public class WebDriverActions : IWebDriverActions
{
    private readonly IDriverFixture _driverFixture;
    private readonly TestSettings _testSettings;
    private readonly ITestOutputHelper _outputHelper;
    private readonly Lazy<WebDriverWait> _webDriverWait;

    public WebDriverActions(IDriverFixture driverFixture, TestSettings testSettings, ITestOutputHelper outputHelper)
    {
        _driverFixture = driverFixture;
        _testSettings = testSettings;
        _outputHelper = outputHelper;
        _webDriverWait = new Lazy<WebDriverWait>(GetWaitDriver);
    }

    public IWebDriver Driver => _driverFixture.Driver;

    private WebDriverWait GetWaitDriver()
    {
        return new WebDriverWait(Driver, timeout: TimeSpan.FromSeconds(_testSettings.TimeoutInterval ?? 10))
        {
            PollingInterval = TimeSpan.FromSeconds(_testSettings.TimeoutPollingInterval ?? 1)
        };
    }

    private void HandleAlerts()
    {
        try
        {
            IAlert alert = Driver.SwitchTo().Alert();

            // Handle the alert based on its type
            if (alert.Text!.Contains("Are you sure")) // Example: Confirm alert
            {
                alert.Dismiss(); // Click "Cancel"
            }
            else if (alert.Text.Contains("Enter your name")) // Example: Prompt alert
            {
                alert.SendKeys("John Doe"); // Enter text
                alert.Accept(); // Click "OK"
            }
            else // Example: JavaScript alert
            {
                alert.Accept(); // Click "OK"
            }

        }
        catch (NoAlertPresentException)
        {
            // No alert present, continue
        }
    }

    private T RetryWithAlertHandling<T>(Func<T> action, int maxRetries = 3, int delayBetweenRetriesMs = 500)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                WaitForPageToLoad(_driverFixture.Driver); // Ensure the page is fully loaded after refresh
                // Check for "Something Went Wrong" message
                if (!FindElementsContainingText(_driverFixture.Driver, "Something Went Wrong").IsNullOrEmpty())
                {
                    _driverFixture.Driver.Navigate().Refresh();
                    WaitForPageToLoad(_driverFixture.Driver); // Ensure the page is fully loaded after refresh
                    Thread.Sleep(2000);
                    continue;
                }
                return action();
            }
            catch (StaleElementReferenceException)
            {
                // Handle stale element specifically
                if (attempt == maxRetries) throw;

                // Refresh page and retry
                Thread.Sleep(3000);
                _driverFixture.Driver.Navigate().GoToUrl(_driverFixture.Driver.Url);
                Thread.Sleep(2000);
                // Explicitly retry the action
                try
                {
                    return action();
                }
                catch (StaleElementReferenceException)
                {
                    // If it still fails, continue to next retry
                    continue;
                }
            }
            catch (Exception ex) when (
                ex is UnhandledAlertException ||
                ex is ElementClickInterceptedException ||
                ex is ElementNotInteractableException ||
                ex is NoSuchElementException)
            {
                if (attempt == maxRetries) throw;

                // Handle other exceptions
                HandleAlerts();
                Thread.Sleep(delayBetweenRetriesMs * attempt);
            }
        }
        throw new Exception("Max retries reached");
    }

    private void WaitForPageToLoad(IWebDriver driver, int timeoutInSeconds = 30)
    {
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
        wait.Until(d =>
        {
            try
            {
                var jsExecutor = (IJavaScriptExecutor)d;
                return jsExecutor.ExecuteScript("return document.readyState").ToString() == "complete";
            }
            catch (WebDriverException ex)
            {
                // If navigation interrupts the script execution, return false to retry
                Console.WriteLine($"Navigation interrupted: {ex.Message}");
                return false;
            }
        });

        // Add small delay to ensure DOM is stable / letting this here to keep watching how this method is functioning 
        //Thread.Sleep(500);
    }

    public IWebElement FindElement(By elementLocator)
    {
        try
        {
            return RetryWithAlertHandling(() =>
                _webDriverWait.Value.Until(driver =>
                {
                    var elements = driver.FindElements(elementLocator);
                    return elements.Count > 0 ? elements.First() : null;
                }))!;
        }
        catch (WebDriverTimeoutException ex)
        {
            throw new NotFoundException($"Element not found with locator: {elementLocator}", ex);
        }
    }

    public IEnumerable<IWebElement> FindElements(By elementLocator)
    {
        try
        {
            return RetryWithAlertHandling(() =>
                _webDriverWait.Value.Until(driver =>
                {
                    var elements = driver.FindElements(elementLocator);
                    return elements.Count > 0 ? elements : Enumerable.Empty<IWebElement>();
                }));
        }
        catch (WebDriverTimeoutException)
        {
            // Return empty collection instead of throwing an exception
            return Enumerable.Empty<IWebElement>();
        }
    }

    public void ClickElement(IWebElement element, int maxRetries = 5, int delayBetweenRetriesMs = 3000)
    {
        WebDriverWait wait = new WebDriverWait(_driverFixture.Driver, TimeSpan.FromMilliseconds(delayBetweenRetriesMs * maxRetries));

        RetryWithAlertHandling(() =>
        {
            try
            {
                // Primary method: Standard Selenium click
                wait.Until(driver => element.Enabled);
                element.Click();
            }
            catch (Exception ex) when (
                ex is JavaScriptException ||
                ex is ElementNotInteractableException)
            {
                // Fallback to Actions class click
                new Actions(_driverFixture.Driver)
                    .MoveToElement(element)
                    .Click()
                    .Perform();
            }
            return true;
        }, maxRetries, delayBetweenRetriesMs);
    }

    public void SendKeysToElement(IWebElement element, string keys, int maxRetries = 5, int delayBetweenRetriesMs = 3000)
    {
        WebDriverWait wait = new WebDriverWait(_driverFixture.Driver, TimeSpan.FromMilliseconds(delayBetweenRetriesMs * maxRetries));

        RetryAction(() =>
        {
            RetryWithAlertHandling(() =>
            {
                try
                {
                    // Primary method: Standard Selenium SendKeys
                    wait.Until(driver => element.Enabled);
                    element.SendKeys(keys);
                }
                catch (Exception ex) when (
                ex is JavaScriptException ||
                ex is ElementNotInteractableException)
                {
                    // Fallback: Actions class interaction
                    new Actions(_driverFixture.Driver)
                        .MoveToElement(element)
                        .Click()
                        .KeyDown(Keys.Control)
                        .SendKeys("a")
                        .KeyUp(Keys.Control)
                        .SendKeys(Keys.Delete)
                        .SendKeys(keys)
                        .Perform();
                }
                return true;
            }, maxRetries, delayBetweenRetriesMs);

            if (IsFileInputElement(element))
            {
                return VerifyFileUpload(element, keys);
            }
            else if (IsSpecialKeyCombination(keys))
            {
                return VerifySpecialKeyAction(element, keys);
            }
            return element.GetDomProperty("value").Contains(keys);
        }, 3);
    }

    // Helper method to detect special key combinations
    private bool IsSpecialKeyCombination(string keys)
    {
        // Add more key combinations if needed
        return keys.Contains(Keys.Control) || keys.Contains(Keys.Shift) || keys.Contains(Keys.Alt);
    }

    // Helper method to check if the element is a file input
    private bool IsFileInputElement(IWebElement element)
    {
        return element.GetDomProperty("type").Equals("file", StringComparison.OrdinalIgnoreCase);
    }

    // Verification logic for special key actions (e.g., Control + A)
    private bool VerifySpecialKeyAction(IWebElement element, string keys)
    {
        if (keys.Contains(Keys.Control + "a"))
        {
            // Verify the element is focused after the operation
            return element.Equals(_driverFixture.Driver.SwitchTo().ActiveElement());
        }
        return true; // Assume success for now for any special keys if no other condition applies
    }

    // Verification logic for file upload
    private bool VerifyFileUpload(IWebElement element, string filePath)
    {
        // For file uploads, we can verify if the file input element has the correct file path
        return element.GetDomProperty("value").Contains(Path.GetFileName(filePath));
    }


    public void WaitUntilElementNotDisplayed(By locator, int timeoutInSeconds = 20)
    {
        RetryWithAlertHandling(() =>
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutInSeconds));
            wait.Until(ExpectedConditions.InvisibilityOfElementLocated(locator));
            return true;
        });
    }

    public string GetElementText(IWebElement element, int maxRetries = 5, int delayBetweenRetriesMs = 3000)
    {
        return RetryWithAlertHandling(() => element.Text, maxRetries, delayBetweenRetriesMs);
    }

    public bool IsElementDisplayed(IWebElement element, int maxRetries = 5, int delayBetweenRetriesMs = 3000)
    {
        Thread.Sleep(1000);
        return RetryWithAlertHandling(() => element.Displayed, maxRetries, delayBetweenRetriesMs);
    }

    public IWebElement WaitUntilElementWithTextAppears(string text, int timeoutInSeconds = 30)
    {
        return RetryWithAlertHandling(() =>
        {
            WebDriverWait wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutInSeconds));
            IWebElement? element = wait.Until(drv =>
            {
                var elements = drv.FindElements(By.XPath($"//*[contains(text(), '{text}')]"));
                return elements.FirstOrDefault(e => IsElementDisplayed(e));
            });
            return element ?? throw new WebDriverTimeoutException($"Element containing text '{text}' did not appear after {timeoutInSeconds} seconds.");
        });
    }

    public IWebElement FindElement(ISearchContext ctx, By by)
    {
        try
        {
            return RetryWithAlertHandling(() => ctx.FindElement(by));
        }
        catch (NoSuchElementException)
        {
            return null!;
        }
    }

    public ReadOnlyCollection<IWebElement> FindElements(ISearchContext ctx, By by)
    {
        try
        {
            return RetryWithAlertHandling(() => ctx.FindElements(by));
        }
        catch (NoSuchElementException)
        {
            return null!;
        }
    }

    public IWebElement FindElementCaseInsensitive(IWebDriver driver, string searchValue)
    {
        // Convert searchValue to lowercase once for use in the XPath
        string lowerCaseSearchValue = searchValue.ToLower();

        // Use translate to perform a case-insensitive search
        var elements = driver.FindElements(By.XPath($"//*[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '{lowerCaseSearchValue}')]"));
        if (elements.Count > 0)
        {
            return elements[0];
        }

        return null!;
    }

    public IReadOnlyCollection<IWebElement> FindElementsContainingText(IWebDriver driver, string searchValue)
    {
        string xPath = $"//*[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '{searchValue.ToLower()}')]";
        return driver.FindElements(By.XPath(xPath));
    }

    // RetryAction method that retries the specified action up to the specified number of times
    public void RetryAction(Func<bool> action, int retries)
    {
        int attempts = 0;
        bool isSuccess = false;

        while (attempts < retries && !isSuccess)
        {
            try
            {
                isSuccess = action();
                if (!isSuccess)
                {
                    attempts++;
                    Thread.Sleep(1000); // Optional: Add a delay between retries
                }
            }
            catch (Exception)
            {
                attempts++;
                Thread.Sleep(1000); // Optional: Add a delay between retries
            }
        }

        if (!isSuccess)
        {
            throw new Exception("Failed to achieve the expected condition after multiple attempts.");
        }
    }

    /// <summary>
    /// Selects an item from a dropdown menu by either text content or position index.
    /// Existing logic for dynamic pattern for ariaLabelledBy when "notFixed" is passed
    /// </summary>
    /// <param name="dataQaid">The data-qaid attribute value of the dropdown container element.</param>
    /// <param name="ariaLabelledBy">The base value for aria-labelledby attribute without '-label' suffix. 
    /// Use 'notFixed' for dynamic aria-labelledby patterns that start with ':'.</param>
    /// <param name="selector">The selection criteria - either the exact text of the item to select or 
    /// a numeric index (starting from 1) representing the item's position in the dropdown.</param>
    /// <example>
    /// Text-based selection: SelectFromDropdown("dropdownQaId", "Calc Type", "Option A")
    /// Index-based selection: SelectFromDropdown("dropdownQaId", "Calc Type", "1")
    /// </example>
    public void SelectFromDropdown(string dataQaid, string ariaLabelledBy, string selector)
    {
        // Create variations of dataQaid for standard cases
        string[] words = dataQaid.Split(' ');
        string camelCaseVariation = string.Concat(words.Select((s, i) => i == 0 ? s.ToLower() : char.ToUpper(s[0]) + s.Substring(1)));
        string pascalCaseVariation = string.Concat(words.Select(s => char.ToUpper(s[0]) + s.Substring(1)));
        string normalVariation = dataQaid.Replace(" ", "");
        string lowercaseFirstVariation = char.ToLower(dataQaid[0]) + dataQaid.Substring(1);
        string uppercaseFirstVariation = char.ToUpper(dataQaid[0]) + dataQaid.Substring(1);
        string lowercaseSecondVariation = words.Length > 1
            ? words[0] + " " + char.ToLower(words[1][0]) + words[1].Substring(1)
            : dataQaid;
        string firstCapitalRestLowerVariation = words.Length > 1
            ? char.ToUpper(words[0][0]) + words[0].Substring(1).ToLower() + words[1].ToLower()
            : char.ToUpper(dataQaid[0]) + dataQaid.Substring(1).ToLower();

        // Locate and click the dropdown menu with variations
        IWebElement dropdownMenu = FindElement(By.XPath(
            $"//div[@data-qaid='{dataQaid}']//input | " +
            $"//div[@data-qaid='{camelCaseVariation}']//input | " +
            $"//div[@data-qaid='{pascalCaseVariation}']//input | " +
            $"//div[@data-qaid='{normalVariation}']//input | " +
            $"//div[@data-qaid='{lowercaseFirstVariation}']//input | " +
            $"//div[@data-qaid='{uppercaseFirstVariation}']//input | " +
            $"//div[@data-qaid='{lowercaseSecondVariation}']//input | " +
            $"//div[@data-qaid='{firstCapitalRestLowerVariation}']//input | " +
            $"//div[@data-qaid='{dataQaid}']//div | " +
            $"//div[@data-qaid='{camelCaseVariation}']//div | " +
            $"//div[@data-qaid='{pascalCaseVariation}']//div | " +
            $"//div[@data-qaid='{normalVariation}']//div | " +
            $"//div[@data-qaid='{lowercaseFirstVariation}']//div | " +
            $"//div[@data-qaid='{uppercaseFirstVariation}']//div | " +
            $"//div[@data-qaid='{lowercaseSecondVariation}']//div | " +
            $"//div[@data-qaid='{firstCapitalRestLowerVariation}']//div"));

        ClickElement(dropdownMenu);

        string xpathForItem;
        bool isNumeric = int.TryParse(selector, out int index);
        if (ariaLabelledBy.Equals("notFixed", StringComparison.OrdinalIgnoreCase))
        {
            // Existing logic for dynamic pattern when "notFixed" is passed
            xpathForItem = isNumeric
                ? $"//*[starts-with(@aria-labelledby, ':') and substring(@aria-labelledby, string-length(@aria-labelledby) - 6) = ':-label']//li[{index}]"
                : $"//*[starts-with(@aria-labelledby, ':') and substring(@aria-labelledby, string-length(@aria-labelledby) - 6) = ':-label']//li[text() = '{selector}']";
        }
        else
        {
            // Existing logic for ariaLabelledBy variations
            string[] ariaWords = ariaLabelledBy.Split(' ');
            string ariaCamelCaseVariation = string.Concat(ariaWords.Select((s, i) => i == 0 ? s.ToLower() : char.ToUpper(s[0]) + s.Substring(1)));
            string ariaPascalCaseVariation = string.Concat(ariaWords.Select(s => char.ToUpper(s[0]) + s.Substring(1)));
            string ariaNormalVariation = ariaLabelledBy.Replace(" ", "");
            string ariaLowercaseFirstVariation = char.ToLower(ariaLabelledBy[0]) + ariaLabelledBy.Substring(1);
            string ariaUppercaseFirstVariation = char.ToUpper(ariaLabelledBy[0]) + ariaLabelledBy.Substring(1);
            string ariaLowercaseSecondVariation = ariaWords.Length > 1
                ? ariaWords[0] + " " + char.ToLower(ariaWords[1][0]) + ariaWords[1].Substring(1)
                : ariaLabelledBy;
            string ariaFirstCapitalRestLowerVariation = ariaWords.Length > 1
                ? char.ToUpper(ariaWords[0][0]) + ariaWords[0].Substring(1).ToLower() + ariaWords[1].ToLower()
                : char.ToUpper(ariaLabelledBy[0]) + ariaLabelledBy.Substring(1).ToLower();

            // Construct XPath with all standard variations, using either index or text based on input
            string itemSelector = isNumeric ? $"li[{index}]" : $"li[text() = '{selector}']";
            xpathForItem = $"//*[@aria-labelledby='{ariaCamelCaseVariation}Id-label' or " +
                $"@aria-labelledby='{ariaCamelCaseVariation}ID-label' or " +
                $"@aria-labelledby='{ariaPascalCaseVariation}Id-label' or " +
                $"@aria-labelledby='{ariaPascalCaseVariation}ID-label' or " +
                $"@aria-labelledby='{ariaNormalVariation}-label' or " +
                $"@aria-labelledby='{ariaLabelledBy}-label' or " +
                $"@aria-labelledby='{ariaCamelCaseVariation}-label' or " +
                $"@aria-labelledby='{ariaPascalCaseVariation}-label' or " +
                $"@aria-labelledby='{ariaLowercaseFirstVariation}-label' or " +
                $"@aria-labelledby='{ariaUppercaseFirstVariation}-label' or " +
                $"@aria-labelledby='{ariaLowercaseSecondVariation}-label' or " +
                $"@aria-labelledby='{ariaFirstCapitalRestLowerVariation}Id-label' or " +
                $"@aria-labelledby='{ariaFirstCapitalRestLowerVariation}ID-label']//{itemSelector}";
        }

        // Locate and click the dropdown item
        IWebElement dropdownItem = FindElement(By.XPath(xpathForItem));
        ClickElement(dropdownItem);

        // Wait until the item is selected
        WaitUntilElementNotDisplayed(By.XPath(xpathForItem));
    }

    public void WaitForPageToLoadCorrectly()
    {
        const int maxRetries = 5;
        const int retryDelayMs = 2000;
        var wait = new WebDriverWait(_driverFixture.Driver, TimeSpan.FromSeconds(10));

        for (int refreshCount = 0; refreshCount < maxRetries; refreshCount++)
        {
            try
            {
                // First, wait for either the grid to load or "No rows" message to appear
                wait.Until(driver =>
                {
                    try
                    {
                        var errorMessage = driver.FindElements(By.XPath("//*[contains(text(),'Something Went Wrong')]"));
                        if (errorMessage.Any())
                        {
                            Console.WriteLine($"Retry {refreshCount + 1}: Detected 'Something Went Wrong'. Refreshing the page...");
                            driver.Navigate().Refresh();
                            Thread.Sleep(retryDelayMs);
                            return false;
                        }

                        // Check for either success condition
                        var hasRows = driver.FindElements(By.XPath("//div[@aria-rowindex='2']")).Any();
                        var hasNoRows = driver.FindElements(By.XPath("//*[contains(text(),'No r')]")).Any();

                        if (hasRows || hasNoRows)
                        {
                            Console.WriteLine($"Page loaded successfully. Found: {(hasRows ? "Data rows" : "No rows message")}");
                            return true;
                        }

                        return false;
                    }
                    catch (StaleElementReferenceException)
                    {
                        return false; // Page is still loading/changing
                    }
                });

                return; // Success case - exit the method
            }
            catch (WebDriverTimeoutException ex)
            {
                Console.WriteLine($"Timeout on attempt {refreshCount + 1}: {ex.Message}");
                if (refreshCount == maxRetries - 1)
                {
                    throw new Exception("Page failed to load correctly after maximum retries", ex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error on attempt {refreshCount + 1}: {ex.Message}");
                if (refreshCount == maxRetries - 1)
                {
                    throw;
                }
            }
        }
    }

    // Method to return web element for a chosen cell in a table
    public IWebElement GetTableCell(int rowNumber, int columnNumber)
    {
        return FindElement(By.XPath($"//div[@aria-rowindex=\"{rowNumber + 1}\"]//div[@aria-colindex=\"{columnNumber}\"]"));
    }

    // Method to return web element for a chosen cell in a table after specified table header
    public IWebElement GetTableCell(int rowNumber, int columnNumber, string tableHeader)
    {
        return FindElement(By.XPath($@"
        //*[contains(text(), '{tableHeader}')] 
        /following::div[@aria-rowindex='{rowNumber + 1}'] 
        //div[@aria-colindex='{columnNumber}']"));
    }

    public IWebElement FindElementInShadowRoot(By shadowHostLocator, By elementLocator)
    {
        try
        {
            return RetryWithAlertHandling(() =>
                _webDriverWait.Value.Until(driver =>
                {
                    try
                    {
                        var shadowHost = driver.FindElement(shadowHostLocator);
                        var shadowRoot = shadowHost.GetShadowRoot();
                        var element = shadowRoot.FindElement(elementLocator);
                        return element.Displayed ? element : null;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }))!;
        }
        catch (WebDriverTimeoutException ex)
        {
            throw new NotFoundException(
                $"Element not found in shadow root. Host locator: {shadowHostLocator}, Element locator: {elementLocator}",
                ex);
        }
    }

    public IEnumerable<IWebElement> FindElementsInShadowRoot(By shadowHostLocator, By elementLocator)
    {
        try
        {
            return RetryWithAlertHandling(() =>
                _webDriverWait.Value.Until(driver =>
                {
                    try
                    {
                        var shadowHost = driver.FindElement(shadowHostLocator);
                        var shadowRoot = shadowHost.GetShadowRoot();
                        var elements = shadowRoot.FindElements(elementLocator);
                        return elements.Count > 0 ? elements : Enumerable.Empty<IWebElement>();
                    }
                    catch (Exception)
                    {
                        return Enumerable.Empty<IWebElement>();
                    }
                }));
        }
        catch (WebDriverTimeoutException)
        {
            return Enumerable.Empty<IWebElement>();
        }
    }

    // Overloads that accept string selectors for convenience
    public IWebElement FindElementInShadowRoot(string shadowHostXPath, string elementCssSelector)
    {
        return FindElementInShadowRoot(
            By.XPath(shadowHostXPath),
            By.CssSelector(elementCssSelector));
    }

    public IEnumerable<IWebElement> FindElementsInShadowRoot(string shadowHostXPath, string elementCssSelector)
    {
        return FindElementsInShadowRoot(
            By.XPath(shadowHostXPath),
            By.CssSelector(elementCssSelector));
    }
}