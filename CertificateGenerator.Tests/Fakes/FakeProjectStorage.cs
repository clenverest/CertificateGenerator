using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Tests.Fakes;

/// <summary>
/// Фейковое хранилище проектов для тестов ViewModel-ей.
/// Позволяет задать поведение: что вернуть, что бросить.
/// </summary>
public sealed class FakeProjectStorage : IProjectStorage
{
    public Project? ProjectToReturnOnLoad { get; set; }
    public Project? ProjectToReturnOnCreate { get; set; }
    public Exception? ExceptionToThrowOnLoad { get; set; }
    public Exception? ExceptionToThrowOnCreate { get; set; }
    public bool ExistsToReturn { get; set; }

    public List<string> LoadedPaths { get; } = new();
    public List<string> CreatedPaths { get; } = new();

    public Exception? ExceptionToThrowOnSetSourceTable { get; set; }
    public string? GetSourceTablePathToReturn { get; set; }
    public List<(string ProjectFolderPath, string SourceFilePath)> SetSourceTableCalls { get; } = new();

    public Project Create(string projectFolderPath, string projectName)
    {
        CreatedPaths.Add(projectFolderPath);
        if (ExceptionToThrowOnCreate is not null)
        {
            throw ExceptionToThrowOnCreate;
        }
        return ProjectToReturnOnCreate ?? new Project(
            projectName, DateTime.Now, string.Empty, Array.Empty<Field>());
    }

    public Project Load(string projectFolderPath)
    {
        LoadedPaths.Add(projectFolderPath);
        if (ExceptionToThrowOnLoad is not null)
        {
            throw ExceptionToThrowOnLoad;
        }
        return ProjectToReturnOnLoad ?? new Project(
            "Проект", DateTime.Now, string.Empty, Array.Empty<Field>());
    }

    public void Save(Project project, string projectFolderPath)
    {
        // В тестах ViewModel-ей сохранение не вызывается.
    }

    public bool Exists(string projectFolderPath)
    {
        return ExistsToReturn;
    }

    public Project SetSourceTable(
        string projectFolderPath,
        Project project,
        string sourceFilePath)
    {
        SetSourceTableCalls.Add((projectFolderPath, sourceFilePath));
        if (ExceptionToThrowOnSetSourceTable is not null)
        {
            throw ExceptionToThrowOnSetSourceTable;
        }

        var fileName = System.IO.Path.GetFileName(sourceFilePath);
        return new Project(
            project.Name,
            project.CreatedAt,
            fileName,
            project.Fields);
    }

    public string? GetSourceTablePath(Project project, string projectFolderPath)
    {
        return GetSourceTablePathToReturn;
    }
}