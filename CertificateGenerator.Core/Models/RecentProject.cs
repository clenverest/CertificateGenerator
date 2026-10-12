namespace CertificateGenerator.Core.Models;

/// <summary>
/// Запись в списке последних проектов.
/// </summary>
public sealed class RecentProject
{
    /// <summary>
    /// Полный путь к папке проекта.
    /// </summary>
    public string ProjectFolderPath { get; }

    /// <summary>
    /// Имя проекта на момент последнего открытия.
    /// </summary>
    public string ProjectName { get; }

    /// <summary>
    /// Когда проект последний раз открывали или создавали.
    /// </summary>
    public DateTime LastOpenedAt { get; }

    public RecentProject(
        string projectFolderPath,
        string projectName,
        DateTime lastOpenedAt)
    {
        if (string.IsNullOrWhiteSpace(projectFolderPath))
        {
            throw new ArgumentException(
                "Путь к папке проекта не может быть пустым.",
                nameof(projectFolderPath));
        }

        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException(
                "Имя проекта не может быть пустым.",
                nameof(projectName));
        }

        ProjectFolderPath = projectFolderPath;
        ProjectName = projectName;
        LastOpenedAt = lastOpenedAt;
    }
}