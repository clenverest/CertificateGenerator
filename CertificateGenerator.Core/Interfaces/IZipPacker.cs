namespace CertificateGenerator.Core.Interfaces;

/// <summary>
/// Упаковка набора файлов в ZIP-архив.
/// Абстракция, чтобы Core-логика не зависела от System.IO.Compression.
/// </summary>
public interface IZipPacker
{
    /// <summary>
    /// Записать ZIP-архив на диск.
    /// </summary>
    /// <param name="zipPath">Полный путь к архиву.</param>
    /// <param name="files">
    /// Файлы: имя внутри архива → содержимое.
    /// Имена уникальны и НЕ содержат путь.
    /// </param>
    void PackToFile(string zipPath, IReadOnlyDictionary<string, byte[]> files);
}