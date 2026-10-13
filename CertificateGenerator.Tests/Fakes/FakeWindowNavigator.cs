using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Tests.Fakes;

public sealed class FakeWindowNavigator : IWindowNavigator
{
    public List<(Project Project, string ProjectFolderPath)> MainNavigations { get; } = new();
    public int StartupNavigations { get; private set; }

    public void NavigateToMain(Project project, string projectFolderPath)
    {
        MainNavigations.Add((project, projectFolderPath));
    }

    public void NavigateToStartup()
    {
        StartupNavigations++;
    }
}
