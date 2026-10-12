namespace CertificateGenerator.Core.Interfaces;

/// <summary>
/// Выбирает подходящий <see cref="ITableReader"/> по пути к файлу
/// (обычно по расширению).
/// </summary>
public interface ITableReaderResolver
{
    /// <summary>
    /// Возвращает ридер, способный прочитать файл по указанному пути.
    /// </summary>
    /// <param name="filePath">Полный путь к файлу.</param>
    /// <returns>Ридер для этого типа файла.</returns>
    /// <exception cref="ArgumentNullException">
    /// Если <paramref name="filePath"/> — <c>null</c> или пустая строка.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Если формат файла не поддерживается
    /// (например, расширение не <c>.xlsx</c> и не <c>.csv</c>).
    /// </exception>
    ITableReader Resolve(string filePath);
}