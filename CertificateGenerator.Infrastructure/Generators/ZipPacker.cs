using System.IO.Compression;
using CertificateGenerator.Core.Interfaces;

namespace CertificateGenerator.Infrastructure.Generators;

/// <summary>
/// Упаковка файлов в ZIP через System.IO.Compression.
/// </summary>
public sealed class ZipPacker : IZipPacker
{
    public void PackToFile(string zipPath, IReadOnlyDictionary<string, byte[]> files)
    {
        if (string.IsNullOrWhiteSpace(zipPath))
        {
            throw new ArgumentException(
                "Путь к архиву не может быть пустым.",
                nameof(zipPath));
        }

        if (files is null)
        {
            throw new ArgumentNullException(nameof(files));
        }

        // Создаём папку на всякий случай.
        var directory = Path.GetDirectoryName(zipPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Перезаписываем существующий архив.
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        using var stream = new FileStream(
            zipPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

        foreach (var (fileName, bytes) in files)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                continue;  // Пропускаем пустые имена.
            }

            if (bytes is null || bytes.Length == 0)
            {
                continue;  // Пропускаем пустые файлы.
            }

            var entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);

            using var entryStream = entry.Open();
            entryStream.Write(bytes, 0, bytes.Length);
        }
    }
}