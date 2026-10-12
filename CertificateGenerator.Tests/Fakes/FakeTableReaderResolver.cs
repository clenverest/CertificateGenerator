using CertificateGenerator.Core.Interfaces;

namespace CertificateGenerator.Tests.Fakes;

/// <summary>
/// Заглушка <see cref="ITableReaderResolver"/>, возвращающая заранее
/// заданный <see cref="ITableReader"/> для поддерживаемых расширений.
/// </summary>
public sealed class FakeTableReaderResolver : ITableReaderResolver
{
    private readonly ITableReader _reader;
    private readonly HashSet<string> _supportedExtensions;

    /// <summary>
    /// Создаёт заглушку.
    /// </summary>
    /// <param name="reader">Ридер, который возвращать для поддерживаемых расширений.</param>
    /// <param name="supportedExtensions">
    /// Расширения (с точкой, например <c>.xlsx</c>), для которых возвращать ридер.
    /// Если не указаны — поддерживаются <c>.xlsx</c> и <c>.csv</c>.
    /// </param>
    public FakeTableReaderResolver(
        ITableReader reader,
        IEnumerable<string>? supportedExtensions = null)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));

        var extensions = supportedExtensions ?? new[] { ".xlsx", ".csv" };
        _supportedExtensions = new HashSet<string>(
            extensions,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public ITableReader Resolve(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        var extension = Path.GetExtension(filePath);

        if (!_supportedExtensions.Contains(extension))
        {
            throw new NotSupportedException(
                $"Формат файла '{extension}' не поддерживается.");
        }

        return _reader;
    }
}