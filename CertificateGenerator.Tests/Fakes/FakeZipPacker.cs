using CertificateGenerator.Core.Interfaces;

namespace CertificateGenerator.Tests.Fakes;

/// <summary>
/// Фейковый упаковщик ZIP: ничего не пишет,
/// только запоминает аргументы.
/// </summary>
public sealed class FakeZipPacker : IZipPacker
{
    /// <summary>Все вызовы: (путь архива, файлы).</summary>
    public List<(string ZipPath,
                 IReadOnlyDictionary<string, byte[]> Files)> Calls { get; } = new();

    /// <summary>Исключение, которое бросит PackToFile, если задано.</summary>
    public Exception? ExceptionToThrow { get; set; }

    public void PackToFile(string zipPath, IReadOnlyDictionary<string, byte[]> files)
    {
        Calls.Add((zipPath, files));

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }
    }
}