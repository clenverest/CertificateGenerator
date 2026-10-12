using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Tests.Fakes;

/// <summary>
/// Фейковое хранилище последних проектов для тестов.
/// Хранит список в памяти.
/// </summary>
public sealed class FakeRecentProjectsStore : IRecentProjectsStore
{
    private readonly List<RecentProject> _entries = new();

    public IReadOnlyList<RecentProject> GetAll() => _entries.ToList();

    public void Add(string projectFolderPath, string projectName)
    {
        _entries.RemoveAll(r => r.ProjectFolderPath == projectFolderPath);
        _entries.Insert(0, new RecentProject(projectFolderPath, projectName, DateTime.Now));
    }

    public void Remove(string projectFolderPath)
    {
        _entries.RemoveAll(r => r.ProjectFolderPath == projectFolderPath);
    }

    public void Clear()
    {
        _entries.Clear();
    }
}