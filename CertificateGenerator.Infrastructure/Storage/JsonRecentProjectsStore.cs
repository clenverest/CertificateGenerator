using System.Text.Encodings.Web;
using System.Text.Json;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Infrastructure.Storage;

/// <summary>
/// Хранилище последних проектов в JSON-файле в %APPDATA%.
/// </summary>
public sealed class JsonRecentProjectsStore : IRecentProjectsStore
{
    /// <summary>
    /// Максимальное количество записей в списке.
    /// </summary>
    internal const int MaxEntries = 10;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _filePath;

    /// <summary>
    /// Создать хранилище в папке по умолчанию
    /// (%APPDATA%\CertificateGenerator\recent.json).
    /// </summary>
    public JsonRecentProjectsStore()
        : this(GetDefaultFilePath())
    {
    }

    /// <summary>
    /// Создать хранилище по указанному пути к файлу.
    /// Используется в тестах, чтобы не трогать %APPDATA%.
    /// </summary>
    internal JsonRecentProjectsStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "Путь к файлу не может быть пустым.",
                nameof(filePath));
        }

        _filePath = filePath;
    }

    public IReadOnlyList<RecentProject> GetAll()
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<RecentProject>();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<RecentProject>();
            }

            var entries = JsonSerializer.Deserialize<List<RecentProject>>(
                json, SerializerOptions);

            return entries ?? (IReadOnlyList<RecentProject>)Array.Empty<RecentProject>();
        }
        catch (JsonException)
        {
            // Файл битый — считаем список пустым. Не ломаем приложение
            // из-за одного сломанного файла настроек.
            return Array.Empty<RecentProject>();
        }
    }

    public void Add(string projectFolderPath, string projectName)
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

        var current = GetAll().ToList();

        // Удаляем существующую запись с таким же путём (если есть).
        current.RemoveAll(r => string.Equals(
            r.ProjectFolderPath,
            projectFolderPath,
            StringComparison.OrdinalIgnoreCase));

        // Вставляем новую в начало.
        current.Insert(0, new RecentProject(
            projectFolderPath,
            projectName,
            DateTime.Now));

        // Обрезаем до лимита.
        if (current.Count > MaxEntries)
        {
            current.RemoveRange(MaxEntries, current.Count - MaxEntries);
        }

        Save(current);
    }

    public void Remove(string projectFolderPath)
    {
        if (string.IsNullOrWhiteSpace(projectFolderPath))
        {
            return;
        }

        var current = GetAll().ToList();
        var removed = current.RemoveAll(r => string.Equals(
            r.ProjectFolderPath,
            projectFolderPath,
            StringComparison.OrdinalIgnoreCase));

        if (removed > 0)
        {
            Save(current);
        }
    }

    public void Clear()
    {
        Save(Array.Empty<RecentProject>());
    }

    // ---------------------------------------------------------------------
    // Приватные методы
    // ---------------------------------------------------------------------

    private void Save(IReadOnlyList<RecentProject> entries)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(entries, SerializerOptions);
        File.WriteAllText(_filePath, json);
    }

    private static string GetDefaultFilePath()
    {
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "CertificateGenerator", "recent.json");
    }
}