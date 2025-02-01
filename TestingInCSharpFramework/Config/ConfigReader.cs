using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestingInCSharpFramework.Config;

public static class ConfigReader
{
    public static TestSettings ReadConfig()
    {
        string configFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/appsettings.json";

        string configFileContent = File.ReadAllText(configFilePath);

        JsonSerializerOptions options = new JsonSerializerOptions();
        options.PropertyNameCaseInsensitive = true;
        options.Converters.Add(new JsonStringEnumConverter());

        TestSettings? settings = JsonSerializer.Deserialize<TestSettings>(configFileContent, options);

        return settings ?? throw new Exception("Failed to deserialize the configuration file.");
    }
}
