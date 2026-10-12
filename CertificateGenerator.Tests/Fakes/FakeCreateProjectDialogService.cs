using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Tests.Fakes;

public sealed class FakeCreateProjectDialogService : ICreateProjectDialogService
{
    public CreateProjectResult? ResultToReturn { get; set; }
    public int ShowCallCount { get; private set; }

    public CreateProjectResult? Show()
    {
        ShowCallCount++;
        return ResultToReturn;
    }
}