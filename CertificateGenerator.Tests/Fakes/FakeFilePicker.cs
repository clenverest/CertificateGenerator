using CertificateGenerator.Core.Interfaces;

namespace CertificateGenerator.Tests.Fakes;

/// <summary>
/// Фейковый выбор файла: возвращает заранее заданный путь.
/// Запоминает запрошенные заголовки и фильтры.
/// </summary>
public sealed class FakeFilePicker : IFilePicker
{
    /// <summary>Путь, который вернёт PickFile. null = отмена.</summary>
    public string? FileToReturn { get; set; }

    /// <summary>Путь, который вернёт PickSaveFile. null = отмена.</summary>
    public string? SaveFileToReturn { get; set; }

    /// <summary>Запросы открытия: (заголовок, фильтр).</summary>
    public List<(string Title, string Filter)> Requests { get; } = new();

    /// <summary>
    /// Запросы сохранения: (заголовок, фильтр, имя по умолчанию, папка).
    /// </summary>
    public List<(string Title,
                 string Filter,
                 string DefaultFileName,
                 string? DefaultFolderPath)> SaveRequests { get; } = new();

    public string? PickFile(string title, string filter)
    {
        Requests.Add((title, filter));
        return FileToReturn;
    }

    public string? PickSaveFile(
        string title,
        string filter,
        string defaultFileName,
        string? defaultFolderPath = null)
    {
        SaveRequests.Add((title, filter, defaultFileName, defaultFolderPath));
        return SaveFileToReturn;
    }
}