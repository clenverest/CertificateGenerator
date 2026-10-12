using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Interfaces;

/// <summary>
/// Валидатор: проверяет таблицу и поля проекта, возвращает список проблем.
/// </summary>
public interface IValidator
{
    /// <summary>
    /// Провести валидацию.
    /// </summary>
    /// <param name="table">Таблица с данными участников.</param>
    /// <param name="fields">Список полей проекта.</param>
    /// <returns>
    /// Список найденных проблем. Пустой список, если проблем нет.
    /// </returns>
    IReadOnlyList<ValidationIssue> Validate(
        TableData table,
        IReadOnlyList<Field> fields);
}