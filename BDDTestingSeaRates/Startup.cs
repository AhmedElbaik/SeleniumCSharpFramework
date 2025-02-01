using Autofac;
using Reqnroll.Autofac;
using Reqnroll.Autofac.ReqnrollPlugin;
using TestingInCSharpFramework.Config;
using TestingInCSharpFramework.Utils;
using Xunit.Sdk;
namespace BDDTestingSeaRates;

public class Startup
{
    private static readonly ITestOutputHelper _outputHelper = new TestOutputHelper(); // Initialize _outputHelper here
    // Create a static instance of AuthManager to ensure it's truly singleton
    private static readonly IAuthManager _authManager = new AuthManager();

    [ScenarioDependencies]
    public static void SetupScenarioContainer(ContainerBuilder containerBuilder)
    {
        // Register scenario scoped runtime dependencies
        containerBuilder
           .RegisterInstance(ConfigReader.ReadConfig())
           .As<TestSettings>()
           .SingleInstance();

        // Register AuthManager as singleton using the static instance
        containerBuilder
            .RegisterInstance(_authManager)
            .As<IAuthManager>()
            .SingleInstance();

        // Correctly register ITestOutputHelper
        containerBuilder
            .RegisterInstance(_outputHelper)
            .As<ITestOutputHelper>()
            .SingleInstance();

        containerBuilder
            .RegisterType<DockerUtils>()
            .As<IDockerUtils>()
            .InstancePerLifetimeScope();

        containerBuilder
           .RegisterType<DriverFixture>()
           .As<IDriverFixture>()
           .InstancePerLifetimeScope();

        containerBuilder
           .RegisterType<WebDriverActions>()
           .As<IWebDriverActions>()
           .InstancePerLifetimeScope();

        containerBuilder
           .RegisterType<FileUtils>()
           .As<IFileUtils>()
           .InstancePerLifetimeScope();

        containerBuilder
           .RegisterType<DB3FilesUtils>()
           .As<IDB3FilesUtils>()
           .InstancePerLifetimeScope();

        containerBuilder
          .RegisterType<SqlDBUtils>()
          .As<ISqlDBUtils>()
          .InstancePerLifetimeScope();

        containerBuilder
          .RegisterType<DateUtils>()
          .As<IDateUtils>()
          .InstancePerLifetimeScope();

        containerBuilder
           .RegisterType<LandingPage>()
           .As<ILandingPage>()
           .InstancePerLifetimeScope();

        containerBuilder
           .RegisterType<LoginPage>()
           .As<ILoginPage>()
           .InstancePerLifetimeScope();

        // Register all binding classes in the assembly
        containerBuilder.AddReqnrollBindings<Startup>();
    }
}
