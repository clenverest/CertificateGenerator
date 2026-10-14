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

    /// <summary>
    /// Все вызовы Save: (папка проекта, сохранённый проект).
    /// </summary>
    public List<(string ProjectFolderPath, Project Project)> SaveCalls { get; } = new();

    /// <summary>
    /// Исключение, которое бросит Save, если задано.
    /// </summary>
    public Exception? ExceptionToThrowOnSave { get; set; }

    /// <summary>
    /// Все вызовы SetTemplate: (папка проекта, шаблон).
    /// </summary>
    public List<(string ProjectFolderPath, TemplateInfo Template)> SetTemplateCalls { get; } = new();

    /// <summary>
    /// Исключение, которое бросит SetTemplate, если задано.
    /// </summary>
    public Exception? ExceptionToThrowOnSetTemplate { get; set; }

    /// <summary>
    /// Все вызовы SetPlacements: (папка проекта, размещения).
    /// </summary>
    public List<(string ProjectFolderPath, IReadOnlyList<FieldPlacement> Placements)> SetPlacementsCalls { get; } = new();

    /// <summary>
    /// Исключение, которое бросит SetPlacements, если задано.
    /// </summary>
    public Exception? ExceptionToThrowOnSetPlacements { get; set; }

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
        SaveCalls.Add((projectFolderPath, project));

        if (ExceptionToThrowOnSave is not null)
        {
            throw ExceptionToThrowOnSave;
        }
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
            project.Fields,
            project.RowOverrides);
    }

    public string? GetSourceTablePath(Project project, string projectFolderPath)
    {
        return GetSourceTablePathToReturn;
    }

    public Project SetTemplate(
        string projectFolderPath,
        Project project,
        TemplateInfo template)
    {
        SetTemplateCalls.Add((projectFolderPath, template));
        if (ExceptionToThrowOnSetTemplate is not null)
        {
            throw ExceptionToThrowOnSetTemplate;
        }

        return new Project(
            project.Name,
            project.CreatedAt,
            project.SourceFileName,
            project.Fields,
            project.RowOverrides,
            template,
            project.Placements);
    }

    public Project SetPlacements(
        string projectFolderPath,
        Project project,
        IReadOnlyList<FieldPlacement> placements)
    {
        SetPlacementsCalls.Add((projectFolderPath, placements));
        if (ExceptionToThrowOnSetPlacements is not null)
        {
            throw ExceptionToThrowOnSetPlacements;
        }

        return new Project(
            project.Name,
            project.CreatedAt,
            project.SourceFileName,
            project.Fields,
            project.RowOverrides,
            project.Template,
            placements);
    }
}