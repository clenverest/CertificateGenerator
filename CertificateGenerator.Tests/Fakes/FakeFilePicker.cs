using CertificateGenerator.Core.Interfaces;

namespace CertificateGenerator.Tests.Fakes;

/// <summary>
/// Фейковый выбор файла: возвращает заранее заданный путь.
/// Запоминает запрошенные заголовки и фильтры.
/// </summary>
public sealed class FakeFilePicker : IFilePicker
{
    public string? FileToReturn { get; set; }

    public List<(string Title, string Filter)> Requests { get; } = new();

    public string? PickFile(string title, string filter)
    {
        Requests.Add((title, filter));
        return FileToReturn;
    }
}