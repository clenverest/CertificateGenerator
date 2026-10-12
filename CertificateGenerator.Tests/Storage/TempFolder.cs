namespace CertificateGenerator.Tests.Storage;

/// <summary>
/// Временная папка для тестов. Создаётся в системной папке временных файлов,
/// автоматически удаляется при вызове Dispose (в том числе при падении теста).
/// </summary>
public sealed class TempFolder : IDisposable
{
    /// <summary>
    /// Полный путь к созданной временной папке.
    /// </summary>
    public string Path { get; }

    public TempFolder()
    {
        var uniqueName = "CertificateGeneratorTests_" + Guid.NewGuid().ToString("N");
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), uniqueName);
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // Намеренно игнорируем любые ошибки удаления.
            // Временная папка — не повод проваливать тест.
        }
    }
}