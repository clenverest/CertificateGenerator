using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Tests.Fakes;

/// <summary>
/// Заглушка <see cref="ITableReader"/>, возвращающая заранее заданный
/// <see cref="TableData"/> независимо от пути к файлу.
/// </summary>
public sealed class FakeTableReader : ITableReader
{
    private readonly TableData _result;

    /// <summary>
    /// Создаёт заглушку, которая всегда возвращает указанную таблицу.
    /// </summary>
    public FakeTableReader(TableData result)
    {
        _result = result ?? throw new ArgumentNullException(nameof(result));
    }

    /// <inheritdoc />
    public TableData Read(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        return _result;
    }
}