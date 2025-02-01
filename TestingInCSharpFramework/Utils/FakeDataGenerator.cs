using Bogus;

namespace TestingInCSharpFramework.Utils;

/// <summary>
/// Generates fake test data for automated testing scenarios
/// </summary>
public static class FakeDataGenerator
{
    /// <summary>
    /// Instance of Faker configured for English locale
    /// </summary>
    public static Faker Faker = new Faker("en");

    /// <summary>
    /// Standard prefix used to identify automated test data
    /// Example: "Auto-Test "
    /// </summary>
    public static string AutoTestStringDataSignature = "Auto-Test ";

    /// <summary>
    /// Standard numeric signature used for automated test data
    /// Example: "155"
    /// </summary>
    public static string AutoTestIntegerDataSignature = "155";

    /// <summary>
    /// Generates a random alphanumeric string of specified length range
    /// Example output: "a7bX9pQ2"
    /// </summary>
    /// <param name="MinValue">Minimum length of string (default: 20)</param>
    /// <param name="MaxValue">Maximum length of string (default: 30)</param>
    /// <returns>Random alphanumeric string</returns>
    public static string GenerateShortString(int MinValue = 20, int MaxValue = 30) => Faker.Random.AlphaNumeric(Faker.Random.Number(MinValue, MaxValue));

    /// <summary>
    /// Generates a longer random alphanumeric string
    /// Example output: "k3mP9nX5vR8wQ1jL7tY4hB2dC6gF8sA9"
    /// </summary>
    /// <param name="MinValue">Minimum length of string (default: 70)</param>
    /// <param name="MaxValue">Maximum length of string (default: 80)</param>
    /// <returns>Random long alphanumeric string</returns>
    public static string GenerateLongString(int MinValue = 70, int MaxValue = 80) => Faker.Random.AlphaNumeric(Faker.Random.Number(MinValue, MaxValue));

    /// <summary>
    /// Generates a random integer as string within specified range
    /// Example output: "42789"
    /// </summary>
    /// <param name="MinValue">Minimum value (default: 10)</param>
    /// <param name="MaxValue">Maximum value (default: 9999999)</param>
    /// <returns>Random integer as string</returns>
    public static string GenerateRandomIntegerString(int MinValue = 10, int MaxValue = 9999999) => Faker.Random.Number(MinValue, MaxValue).ToString();

    /// <summary>
    /// Generates a random country name
    /// Example output: "France"
    /// </summary>
    /// <returns>Random country name</returns>
    public static string GenerateRandomCountry() => Faker.Address.Country();

    /// <summary>
    /// Generates a random two-letter country code
    /// Example output: "FR"
    /// </summary>
    /// <returns>Random country code</returns>
    public static string GenerateRandomCountryCode() => Faker.Address.CountryCode();

    /// <summary>
    /// Generates a random three-letter currency code
    /// Example output: "EUR"
    /// </summary>
    /// <returns>Random currency code</returns>
    public static string GenerateRandomCurrencyCode() => Faker.Finance.Currency().Code;

    /// <summary>
    /// Generates a random currency symbol
    /// Example output: "€"
    /// </summary>
    /// <returns>Random currency symbol</returns>
    public static string GenerateRandomCurrencySymbol() => Faker.Finance.Currency().Symbol;

    /// <summary>
    /// Generates a random currency full name
    /// Example output: "Euro"
    /// </summary>
    /// <returns>Random currency name</returns>
    public static string GenerateRandomCurrencyFullName() => Faker.Finance.Currency().Description;

    /// <summary>
    /// Generates a past date in MM/dd/yyyy format
    /// Example output: "03/15/2022"
    /// </summary>
    /// <param name="yearsAgo">Number of years to go back (default: 1)</param>
    /// <returns>Past date string</returns>
    public static string GeneratePastDateString(int yearsAgo = 1)
    => Faker.Date.Past(yearsAgo).ToString("MM/dd/yyyy");

    /// <summary>
    /// Generates a future date in MM/dd/yyyy format
    /// Example output: "03/15/2024"
    /// </summary>
    /// <param name="yearsAhead">Number of years to go forward (default: 1)</param>
    /// <returns>Future date string</returns>
    public static string GenerateFutureDateString(int yearsAhead = 1)
        => Faker.Date.Future(yearsAhead).ToString("MM/dd/yyyy");
}