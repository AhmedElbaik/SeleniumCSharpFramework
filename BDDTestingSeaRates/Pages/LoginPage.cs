namespace BDDTestingSeaRates.Pages;

public class LoginPage : ILoginPage
{
    private readonly IWebDriverActions _driver;
    private ITestOutputHelper _outputHelper;

    public LoginPage(IWebDriverActions driver, ITestOutputHelper outputHelper)
    {
        _driver = driver;
        _outputHelper = outputHelper;
    }

    private IWebElement EmailField => _driver.FindElement(By.XPath("//input[@name=\"login\"]"));
    private IWebElement PasswordField => _driver.FindElement(By.XPath("//input[@name=\"password\"]"));
    private IWebElement SignInButton => _driver.FindElement(By.XPath("//button[@type=\"submit\"]"));


    public void Login(string email, string password)
    {
        _driver.SendKeysToElement(EmailField, email);
        _driver.SendKeysToElement(PasswordField, password);
        _driver.ClickElement(SignInButton);
    }
}
