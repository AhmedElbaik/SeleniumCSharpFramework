namespace BDDTestingSeaRates.Pages
{
    public interface ILandingPage
    {
        void FillFromInputField(string text);
        void FillToInputField(string text);
        void AcceptAllCookies();
        void DenyAllCookies();
        void SelectDate(string date);
        void ClickSuggestedCity(string city);
        void NavigateToLoginPage();
    }
}