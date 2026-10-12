using ClosedXML.Excel;

namespace CertificateGenerator.Tests.Readers;

/// <summary>
/// Создаёт временные .xlsx-файлы для тестов и удаляет их при Dispose.
/// </summary>
public sealed class TestXlsxFactory : IDisposable
{
    private readonly string _tempDirectory;

    public TestXlsxFactory()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "CertificateGeneratorTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// Создаёт .xlsx-файл и наполняет его через указанное действие.
    /// </summary>
    public string CreateFile(string fileName, Action<IXLWorksheet> fill)
    {
        ArgumentNullException.ThrowIfNull(fill);

        string fullPath = Path.Combine(_tempDirectory, fileName);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Sheet1");
        fill(worksheet);
        workbook.SaveAs(fullPath);

        return fullPath;
    }

    /// <summary>
    /// Создаёт простой .xlsx с заголовками в первой строке и данными ниже.
    /// </summary>
    public string CreateSimpleFile(
        string fileName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        return CreateFile(fileName, worksheet =>
        {
            for (int i = 0; i < headers.Count; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            for (int r = 0; r < rows.Count; r++)
            {
                for (int c = 0; c < rows[r].Count; c++)
                {
                    worksheet.Cell(r + 2, c + 1).Value = rows[r][c];
                }
            }
        });
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
                // Файл занят — не критично для тестов, оставляем как есть.
            }
        }
    }
}