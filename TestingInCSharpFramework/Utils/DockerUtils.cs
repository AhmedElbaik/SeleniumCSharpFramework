using System.Diagnostics;
using TestingInCSharpFramework.Config;

namespace TestingInCSharpFramework.Utils;

public interface IDockerUtils
{
    void DeleteFileInDockerFolder(string fileName);
}

public class DockerUtils : IDockerUtils
{
    private readonly TestSettings _testSettings;

    public DockerUtils(TestSettings testSettings)
    {
        _testSettings = testSettings;
    }

    public void DeleteFileInDockerFolder(string fileName)
    {
        string filePath = $"{_testSettings.DockerSharedFolder}/{fileName}";
        File.Delete(filePath);
    }
}
