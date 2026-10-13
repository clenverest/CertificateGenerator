using System.Text.Encodings.Web;
using System.Text.Json;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Infrastructure.Storage;

/// <summary>
/// Хранилище проектов в формате JSON.
/// Проект — папка, внутри которой лежит project.json и подпапки
/// source/, template/, output/.
/// </summary>
public sealed class JsonProjectStorage : IProjectStorage
{
    internal const string ProjectFileName = "project.json";
    internal const string SourceFolderName = "source";
    internal const string TemplateFolderName = "template";
    internal const string OutputFolderName = "output";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // ---------------------------------------------------------------------
    // IProjectStorage
    // ---------------------------------------------------------------------

    public Project Create(string projectFolderPath, string projectName)
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

        if (Directory.Exists(projectFolderPath) &&
            Directory.EnumerateFileSystemEntries(projectFolderPath).Any())
        {
            throw new InvalidOperationException(
                $"Папка \"{projectFolderPath}\" не пуста. " +
                "Выберите другую папку для нового проекта.");
        }

        Directory.CreateDirectory(projectFolderPath);
        Directory.CreateDirectory(Path.Combine(projectFolderPath, SourceFolderName));
        Directory.CreateDirectory(Path.Combine(projectFolderPath, TemplateFolderName));
        Directory.CreateDirectory(Path.Combine(projectFolderPath, OutputFolderName));

        var project = new Project(
            projectName,
            DateTime.Now,
            string.Empty,
            Array.Empty<Field>());

        Save(project, projectFolderPath);

        return project;
    }

    public Project Load(string projectFolderPath)
    {
        if (string.IsNullOrWhiteSpace(projectFolderPath))
        {
            throw new ArgumentException(
                "Путь к папке проекта не может быть пустым.",
                nameof(projectFolderPath));
        }

        var projectFilePath = Path.Combine(projectFolderPath, ProjectFileName);

        if (!File.Exists(projectFilePath))
        {
            throw new FileNotFoundException(
                $"В папке \"{projectFolderPath}\" нет файла проекта \"{ProjectFileName}\".",
                projectFilePath);
        }

        var json = File.ReadAllText(projectFilePath);
        return Deserialize(json);
    }

    public void Save(Project project, string projectFolderPath)
    {
        if (project is null)
        {
            throw new ArgumentNullException(nameof(project));
        }

        if (string.IsNullOrWhiteSpace(projectFolderPath))
        {
            throw new ArgumentException(
                "Путь к папке проекта не может быть пустым.",
                nameof(projectFolderPath));
        }

        if (!Directory.Exists(projectFolderPath))
        {
            throw new DirectoryNotFoundException(
                $"Папка проекта \"{projectFolderPath}\" не существует.");
        }

        var json = Serialize(project);
        var projectFilePath = Path.Combine(projectFolderPath, ProjectFileName);
        File.WriteAllText(projectFilePath, json);
    }

    public bool Exists(string projectFolderPath)
    {
        if (string.IsNullOrWhiteSpace(projectFolderPath))
        {
            return false;
        }

        var projectFilePath = Path.Combine(projectFolderPath, ProjectFileName);
        return File.Exists(projectFilePath);
    }

    public Project SetSourceTable(
        string projectFolderPath,
        Project project,
        string sourceFilePath)
    {
        if (project is null)
        {
            throw new ArgumentNullException(nameof(project));
        }

        if (string.IsNullOrWhiteSpace(projectFolderPath))
        {
            throw new ArgumentException(
                "Путь к папке проекта не может быть пустым.",
                nameof(projectFolderPath));
        }

        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            throw new ArgumentException(
                "Путь к исходному файлу не может быть пустым.",
                nameof(sourceFilePath));
        }

        if (!Directory.Exists(projectFolderPath))
        {
            throw new DirectoryNotFoundException(
                $"Папка проекта \"{projectFolderPath}\" не существует.");
        }

        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException(
                $"Исходный файл \"{sourceFilePath}\" не найден.",
                sourceFilePath);
        }

        var sourceFolder = Path.Combine(projectFolderPath, SourceFolderName);
        Directory.CreateDirectory(sourceFolder);

        // Удаляем старую таблицу: любые файлы в source/.
        foreach (var oldFile in Directory.EnumerateFiles(sourceFolder))
        {
            File.Delete(oldFile);
        }

        // Копируем новый файл. Имя сохраняем — обычно data.xlsx или data.csv.
        var fileName = Path.GetFileName(sourceFilePath);
        var destinationPath = Path.Combine(sourceFolder, fileName);
        File.Copy(sourceFilePath, destinationPath, overwrite: true);

        // Создаём новый Project с обновлённым SourceFileName.
        var updated = new Project(
            project.Name,
            project.CreatedAt,
            fileName,
            project.Fields);

        Save(updated, projectFolderPath);

        return updated;
    }

    public string? GetSourceTablePath(Project project, string projectFolderPath)
    {
        if (project is null)
        {
            throw new ArgumentNullException(nameof(project));
        }

        if (string.IsNullOrWhiteSpace(projectFolderPath))
        {
            throw new ArgumentException(
                "Путь к папке проекта не может быть пустым.",
                nameof(projectFolderPath));
        }

        if (string.IsNullOrWhiteSpace(project.SourceFileName))
        {
            return null;
        }

        var path = Path.Combine(
            projectFolderPath,
            SourceFolderName,
            project.SourceFileName);

        return File.Exists(path) ? path : null;
    }

    // ---------------------------------------------------------------------
    // Сериализация (internal, тестируется напрямую)
    // ---------------------------------------------------------------------

    internal static string Serialize(Project project)
    {
        if (project is null)
        {
            throw new ArgumentNullException(nameof(project));
        }

        return JsonSerializer.Serialize(project, SerializerOptions);
    }

    internal static Project Deserialize(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        var project = JsonSerializer.Deserialize<Project>(json, SerializerOptions);

        if (project is null)
        {
            throw new InvalidDataException(
                "Не удалось прочитать проект: содержимое файла пустое.");
        }

        return project;
    }
}