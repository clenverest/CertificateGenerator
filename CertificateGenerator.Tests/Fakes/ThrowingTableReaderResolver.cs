using CertificateGenerator.Core.Interfaces;

namespace CertificateGenerator.Tests.Fakes;

/// <summary>
/// Резолвер, который всегда бросает указанное исключение.
/// Используется для тестов обработки ошибок.
/// </summary>
public sealed class ThrowingTableReaderResolver : ITableReaderResolver
{
    private readonly Exception _exception;

    public ThrowingTableReaderResolver(Exception exception)
    {
        _exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }

    public ITableReader Resolve(string filePath)
    {
        throw _exception;
    }
}