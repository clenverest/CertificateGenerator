using CertificateGenerator.Core.Interfaces;

namespace CertificateGenerator.Infrastructure.Readers;

/// <summary>
/// Выбирает <see cref="ITableReader"/> по расширению файла.
/// Поддерживает <c>.xlsx</c> и <c>.csv</c> (без учёта регистра).
/// </summary>
public sealed class TableReaderResolver : ITableReaderResolver
{
    private readonly ClosedXmlTableReader _xlsxReader;
    private readonly CsvTableReader _csvReader;

    /// <summary>
    /// Создаёт резолвер с ридерами по умолчанию.
    /// </summary>
    public TableReaderResolver()
    {
        _xlsxReader = new ClosedXmlTableReader();
        _csvReader = new CsvTableReader();
    }

    /// <inheritdoc />
    public ITableReader Resolve(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string extension = Path.GetExtension(filePath);

        if (extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return _xlsxReader;
        }

        if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return _csvReader;
        }

        throw new NotSupportedException(
            $"Формат файла '{extension}' не поддерживается. " +
            $"Поддерживаются: .xlsx, .csv.");
    }
}