using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using TestingInCSharpFramework.DriverFactory;

namespace TestingInCSharpFramework.Utils;

public interface IDateUtils
{
    bool VerifyDate(IWebElement element, long threshold);
    bool CheckDateFormat(IWebElement element);
    void NavigateToMonthYear(string dateString);
    string GetCurrentDateFormatted();
    string GetCurrentDay();
}

public class DateUtils : IDateUtils
{
    private readonly IWebDriverActions _driver;

    public DateUtils(IWebDriverActions driver)
    {
        _driver = driver;
    }
    /**
     * Verifies, that the difference between the current date and the date value of element is not more than the given threshold
     *
     * @param element The WebElement that should be verified
     * @param threshold Threshold in milliseconds
     * @return True if difference between the two dates is less than or equal to threshold.
     */
    public bool VerifyDate(IWebElement element, long threshold)
    {
        List<string> formatStrings = new List<string>();
        formatStrings.Add("d/M/yyyy h:mm:ss tt");
        formatStrings.Add("M/d/yyyy, h:mm:ss tt");
        formatStrings.Add("yyyy-M-d h:mm:ss tt");
        formatStrings.Add("dd MMM yyyy h:mm:ss tt");
        formatStrings.Add("dd.MM.yyyy, hh:mm:ss");
        // Add more formats as needed

        DateTime currentDate = DateTime.Now;
        foreach (string formatString in formatStrings)
        {
            try
            {
                DateTime actualDate = DateTime.ParseExact(element.Text, formatString, null);
                return (currentDate - actualDate).TotalMilliseconds <= threshold;
            }
            catch (FormatException)
            {
                // Continue to next format if this one fails
            }
        }
        return false;
    }

    /**
     * Checks if the date format matches any of the given format strings
     *
     * @param element The WebElement containing the date string to check
     * @param formatStrings The list of format strings to match against
     * @return True if the date format matches any of the format strings, false otherwise
     */
    public bool CheckDateFormat(IWebElement element)
    {
        List<string> formatStrings = new List<string>();
        formatStrings.Add("d/M/yyyy h:mm:ss tt");
        formatStrings.Add("M/d/yyyy, h:mm:ss tt");
        formatStrings.Add("yyyy-M-d h:mm:ss tt");
        formatStrings.Add("dd MMM yyyy h:mm:ss tt");
        formatStrings.Add("dd.MM.yyyy, hh:mm:ss");
        // Add more formats as needed
        foreach (string formatString in formatStrings)
        {
            try
            {
                DateTime.ParseExact(element.Text, formatString, null);
                return true;
            }
            catch (FormatException)
            {
                // Continue to next format if this one fails
            }
        }
        return false;
    }

    /// <summary>
    /// Navigates to the correct month and year in the calendar dialog based on the provided date.
    /// The method extracts the month and year from the calendar header and navigates to the specified date's month/year.
    /// </summary>
    /// <param name="dateString">The date in one of the accepted formats (e.g., MM/DD/YYYY, M/D/YYYY, etc.) to navigate to the corresponding month and year.</param>
    public void NavigateToMonthYear(string dateString)
    {
        // Parse the target date
        DateTime targetDate = DateTime.Parse(dateString);

        // Find the calendar header label (which contains month/year)
        var calendarHeader = _driver.Driver.FindElement(By.XPath("//*[contains(@class, 'MuiPicker')]//*[contains(@class, 'MuiPickersCalendarHeader-label')]"));

        // Extract the month and year from the header text
        string headerText = calendarHeader.Text.Trim();
        DateTime displayedDate;
        try
        {
            // Try parsing the header directly
            displayedDate = DateTime.Parse(headerText);
        }
        catch
        {
            // Fallback - extract the month and year manually
            var parts = headerText.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                // Ensure we have valid month and year parts
                string monthPart = parts[0].Trim();
                string yearPart = parts[1].Trim();

                if (string.IsNullOrWhiteSpace(monthPart) || string.IsNullOrWhiteSpace(yearPart))
                {
                    throw new FormatException($"Invalid calendar header format: {headerText}");
                }

                string monthYearString = $"1 {monthPart} {yearPart}";
                displayedDate = DateTime.Parse(monthYearString);
            }
            else
            {
                throw new FormatException($"Unable to parse calendar header text: {headerText}");
            }
        }
        // Navigate to the correct month/year
        while (displayedDate.Year != targetDate.Year || displayedDate.Month != targetDate.Month)
        {
            if (displayedDate < targetDate)
            {
                // Click right arrow to move forward
                _driver.Driver.FindElement(By.CssSelector("[data-testid='ArrowRightIcon']")).Click();
            }
            else
            {
                // Click left arrow to move backward
                _driver.Driver.FindElement(By.CssSelector("[data-testid='ArrowLeftIcon']")).Click();
            }

            // Wait for calendar header to update
            var wait = new WebDriverWait(_driver.Driver, TimeSpan.FromSeconds(10));
            calendarHeader = wait.Until(d =>
            {
                var header = d.FindElement(By.XPath("//*[contains(@class, 'MuiPickersCalendarHeader-label')]"));
                return header.Text.Trim() != headerText ? header : null;
            });

            headerText = calendarHeader!.Text.Trim();
            var parts = headerText.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                string monthYearString = $"1 {parts[0]} {parts[1]}";
                displayedDate = DateTime.Parse(monthYearString);
            }
        }
    }
    /// <summary>
    /// Converts a numeric character to its corresponding NumberPad key
    /// </summary>
    /// <param name="c">The numeric character</param>
    /// <returns>The NumberPad key as a string</returns>
    private string GetNumberPadKey(char c)
    {
        return c switch
        {
            '0' => Keys.NumberPad0,
            '1' => Keys.NumberPad1,
            '2' => Keys.NumberPad2,
            '3' => Keys.NumberPad3,
            '4' => Keys.NumberPad4,
            '5' => Keys.NumberPad5,
            '6' => Keys.NumberPad6,
            '7' => Keys.NumberPad7,
            '8' => Keys.NumberPad8,
            '9' => Keys.NumberPad9,
            _ => throw new ArgumentException($"Invalid numeric character: {c}")
        };
    }

    /// <summary>
    /// Converts a string representation of a date to a formatted date string (MM/dd/yyyy)
    /// </summary>
    /// <param name="dateString">The date string to format</param>
    /// <returns>Formatted date string in MM/dd/yyyy format</returns>
    private string FormatDateString(string dateString)
    {
        // Split the input string by possible date delimiters ("/" or "-")
        string[] dateParts = dateString.Split(new[] { '/', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);

        if (dateParts.Length != 3)
        {
            throw new ArgumentException($"Invalid date format: {dateString}");
        }

        // Extract month, day, and year
        string month = dateParts[0].PadLeft(2, '0'); // Add leading zero if necessary
        string day = dateParts[1].PadLeft(2, '0');   // Add leading zero if necessary
        string year = dateParts[2].PadLeft(4, '0');  // Ensure year is 4 digits

        // Return formatted date in MM/dd/yyyy format
        return $"{month}/{day}/{year}";
    }

    // Helper method to get the current date formatted as needed
    public string GetCurrentDateFormatted()
    {
        // Adjust the format to match your application's expected input
        return DateTime.Now.ToString("MM/dd/yyyy"); // Example: "01/05/2025"
    }

    // Helper method to get the current day as a string
    public string GetCurrentDay()
    {
        // Returns the day as a string (e.g., "05" for January 5th)
        return DateTime.Now.ToString("dd"); // Example: "05"
    }
}
