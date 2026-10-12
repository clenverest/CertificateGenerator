using System.Text;

namespace CertificateGenerator.Tests.Readers;

/// <summary>
/// Создаёт временные CSV-файлы для тестов и удаляет их при Dispose.
/// </summary>
public sealed class TestCsvFactory : IDisposable
{
    private readonly string _tempDirectory;

    public TestCsvFactory()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "CertificateGeneratorTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// Создаёт CSV-файл с указанным содержимым и кодировкой.
    /// </summary>
    public string CreateFile(string fileName, string content, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(encoding);

        string fullPath = Path.Combine(_tempDirectory, fileName);
        File.WriteAllText(fullPath, content, encoding);
        return fullPath;
    }

    /// <summary>
    /// Создаёт CSV-файл в UTF-8 без BOM.
    /// </summary>
    public string CreateUtf8File(string fileName, string content)
    {
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        return CreateFile(fileName, content, encoding);
    }

    /// <summary>
    /// Создаёт CSV-файл в UTF-8 с BOM.
    /// </summary>
    public string CreateUtf8BomFile(string fileName, string content)
    {
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return CreateFile(fileName, content, encoding);
    }

    /// <summary>
    /// Создаёт CSV-файл в Windows-1251.
    /// </summary>
    public string CreateWindows1251File(string fileName, string content)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding(1251);
        return CreateFile(fileName, content, encoding);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Файл занят — не критично для тестов.
            }
        }
    }
}