using OpenQA.Selenium;
using System.IdentityModel.Tokens.Jwt;

public class AuthCredentials
{
    public Dictionary<string, string>? LocalStorage { get; set; }
    public Dictionary<string, string>? Cookies { get; set; }
    public DateTime ExpirationTime { get; set; }
}

public interface IAuthManager
{
    void StoreAuthCredentials(IWebDriver driver);
    bool TryRestoreAuth(IWebDriver driver, Func<bool> validationFunction);
    void ClearStoredAuth();
    bool ValidateToken(string token);
}

public class AuthManager : IAuthManager
{
    private AuthCredentials? _storedCredentials;
    private readonly string[] _requiredCookieNames = { "ARRAffinity", "ARRAffinitySameSite", "ai_session", "ai_user" };
    public string? token { get; set; }

    public void StoreAuthCredentials(IWebDriver driver)
    {
        // Extract local storage dynamically
        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
        var keys = (IReadOnlyCollection<object>)js.ExecuteScript("return Object.keys(localStorage);");

        if (keys == null || !keys.Any())
        {
            throw new Exception("No local storage data found.");
        }

        var localStorageData = new Dictionary<string, string>();
        foreach (var key in keys)
        {
            var value = js.ExecuteScript($"return localStorage.getItem('{key}');")?.ToString();
            if (value != null)
            {
                localStorageData.Add(key.ToString()!, value);
            }
        }

        // Extract required cookies
        var cookies = driver.Manage().Cookies.AllCookies
            .Where(c => _requiredCookieNames.Contains(c.Name))
            .ToDictionary(c => c.Name, c => c.Value);

        if (cookies.Count != _requiredCookieNames.Length)
        {
            throw new Exception("Not all required cookies were found.");
        }

        // Validate token if available
        token = localStorageData.GetValueOrDefault("token");
        if (string.IsNullOrEmpty(token) || !ValidateToken(token!))
        {
            throw new Exception("Token is invalid or expired.");
        }

        _storedCredentials = new AuthCredentials
        {
            LocalStorage = localStorageData,
            Cookies = cookies,
            ExpirationTime = DateTime.UtcNow.AddHours(1) // Adjust based on token expiration policy
        };
    }

    public bool TryRestoreAuth(IWebDriver driver, Func<bool> validationFunction)
    {
        if (_storedCredentials == null)
        {
            return false;
        }
        try
        {
            // Inject local storage items
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            foreach (var item in _storedCredentials!.LocalStorage!)
            {
                js.ExecuteScript($"localStorage.setItem('{item.Key}', '{item.Value}');");
            }

            // Add cookies
            foreach (var cookie in _storedCredentials.Cookies!)
            {
                driver.Manage().Cookies.AddCookie(new Cookie(cookie.Key, cookie.Value));
            }

            driver.Navigate().Refresh(); // Apply the restored authentication state
            // Validate Sign Out button is shown
            if (!validationFunction())
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error restoring authentication: {ex.Message}");
            return false;
        }
    }

    public void ClearStoredAuth()
    {
        _storedCredentials = null;
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Check expiration with a 5-minute buffer
            var expirationTime = jwtToken.ValidTo.ToUniversalTime();
            var bufferTime = DateTime.UtcNow.AddMinutes(5);

            return expirationTime > bufferTime;
        }
        catch
        {
            return false;
        }
    }
}