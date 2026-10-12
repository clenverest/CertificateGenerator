using CertificateGenerator.Core.Interfaces;

namespace CertificateGenerator.Tests.Fakes;

/// <summary>
/// Фейковый выбор папки: возвращает заранее заданный путь.
/// </summary>
public sealed class FakeFolderPicker : IFolderPicker
{
    public string? FolderToReturn { get; set; }
    public List<string> RequestedTitles { get; } = new();

    public string? PickFolder(string title)
    {
        RequestedTitles.Add(title);
        return FolderToReturn;
    }
}