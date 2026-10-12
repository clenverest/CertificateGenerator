using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Interfaces;

/// <summary>
/// Читает таблицу из файла.
/// </summary>
/// <remarks>
/// Контракт:
/// <list type="bullet">
/// <item>Первая строка файла — заголовки колонок.</item>
/// <item>Возвращаются только строки данных, без заголовков.</item>
/// <item>Пустые строки в конце файла отбрасываются.</item>
/// <item>Файл с одними заголовками — валидный случай (пустой <c>Rows</c>).</item>
/// </list>
/// </remarks>
public interface ITableReader
{
    /// <summary>
    /// Читает таблицу из файла по указанному пути.
    /// </summary>
    /// <param name="filePath">Полный путь к файлу.</param>
    /// <returns>Прочитанная таблица.</returns>
    /// <exception cref="ArgumentNullException">
    /// Если <paramref name="filePath"/> — <c>null</c> или пустая строка.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Если файл по указанному пути не найден.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// Если файл пуст, не содержит заголовков или повреждён.
    /// </exception>
    /// <exception cref="IOException">
    /// Если файл занят другим процессом или недоступен для чтения.
    /// </exception>
    TableData Read(string filePath);
}