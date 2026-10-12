namespace CertificateGenerator.Core.Models;

/// <summary>
/// Результат диалога создания проекта: путь и имя.
/// </summary>
public sealed class CreateProjectResult
{
    public string FolderPath { get; }
    public string ProjectName { get; }

    public CreateProjectResult(string folderPath, string projectName)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException(
                "Путь к папке не может быть пустым.",
                nameof(folderPath));
        }

        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException(
                "Имя проекта не может быть пустым.",
                nameof(projectName));
        }

        FolderPath = folderPath;
        ProjectName = projectName;
    }
}