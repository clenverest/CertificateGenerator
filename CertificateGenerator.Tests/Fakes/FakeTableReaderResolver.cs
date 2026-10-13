using System.IO;
using CertificateGenerator.Core.Interfaces;

namespace CertificateGenerator.Tests.Fakes;

/// <summary>
/// Фейковый resolver: возвращает заданный ITableReader для .xlsx и .csv,
/// бросает NotSupportedException для остальных расширений.
/// </summary>
public sealed class FakeTableReaderResolver : ITableReaderResolver
{
    private readonly ITableReader _reader;

    public FakeTableReaderResolver(ITableReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public ITableReader Resolve(string filePath)
    {
        if (filePath is null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".csv")
        {
            throw new NotSupportedException(
                $"Формат \"{extension}\" не поддерживается. " +
                "Поддерживаются: .xlsx, .csv.");
        }

        return _reader;
    }
}