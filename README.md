# Selenium C# Testing Project with Reqnroll

This project is a testing suite using Selenium WebDriver with C# (.NET 8.0) and Reqnroll.Xunit as the testing framework.

## Prerequisites

- Visual Studio 2022 (or later)
- .NET 8.0 SDK
- Git

## Getting Started

1. Download & install [Visual Studio](https://visualstudio.microsoft.com/downloads/).
2. Clone the repository with SSH Key: git clone "git@github.com:AhmedElbaik/SeleniumCSharpFramework.git"
3. Open the solution in Visual Studio.

4. Install the Reqnroll extension for Visual Studio:

- Go to `Extensions > Manage Extensions`
- Search for "Reqnroll"
- Download and install the Reqnroll extension
- Restart Visual Studio

4. Restore NuGet packages:

- Right-click on the solution in Solution Explorer
- Select "Restore NuGet Packages"

5. Build the solution:

- Go to `Build > Build Solution` or press `Ctrl+Shift+B`

## Running Tests

1. Set the test settings:

- Open the `appSettings.json` file in the CostbookUITestsBDD project
- Set the `BrowserMode` to "--inprivate" in case of using "EdgeChromium" as "BrowserType"
- Set the `BrowserMode` to "--incognito" in case of using "Chrome" as "BrowserType"

2. Open Test Explorer:

- Go to `Test > Test Explorer` if you can't see the tests in the Test Explorer,
  right click on the BDDTestingSeaRates project and Run tests, when you see the tests, pause the run from the test explorer and then
  search for the tests you want to inspect.

3. Run tests:

- To run all tests, click "Run All" in Test Explorer
- To run a specific test, right-click on the test and select "Run"

## Project Structure

- Test Scenarios can be found in the "Features" folder
- Step Definitions can be found in the "StepDefinitions" folder
- Page Objects can be found in the "Pages" folder with all the elements and methods for the pages
- Test Hooks can be found in the "Hooks" folder with the setup and teardown methods for the tests
- Driver Fixture and Driver Wait classes can be found in the "Driver" folder in TestingInCSharpFramework project refrence
- Utlity classes can be found in the "Utils" folder in TestingInCSharpFramework project refrence with helper methods for the tests
- config.json file can be found in the "Config" folder in TestingInCSharpFramework project refrence
- test settings can be found in the "appSettings.Json" file in BDDTestingSeaRates project refrence with the URL of the website to test

## Writing Tests

1. Create a new feature file:

- Right-click on the project or folder
- Select `Add > New Item`
- Choose "Reqnroll Feature File"
- Write your scenarios using Gherkin syntax

2. Generate step definitions:

- Right-click in the feature file
- Select "Generate Step Definitions"
- Choose where to save the generated file

3. Implement the step definitions in the generated C# file

## Additional Resources

- [Reqnroll Documentation](https://docs.specflow.org/projects/reqnroll/en/latest/)
- [Selenium Documentation](https://www.selenium.dev/documentation/)
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)

## Contributing

- for any questions about how to contribute or concerns please reach out to Ahmed Elbaik

```

```
