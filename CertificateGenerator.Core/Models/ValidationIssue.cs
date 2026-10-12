namespace CertificateGenerator.Core.Models;

/// <summary>
/// Одна проблема, найденная валидатором.
/// </summary>
public sealed class ValidationIssue
{
    /// <summary>
    /// Индекс строки таблицы, к которой относится проблема,
    /// или -1, если проблема не привязана к конкретной строке.
    /// </summary>
    public const int NoRow = -1;

    /// <summary>
    /// Уровень важности проблемы.
    /// </summary>
    public IssueSeverity Severity { get; }

    /// <summary>
    /// Индекс строки таблицы. <see cref="NoRow"/> (-1), если проблема
    /// не привязана к строке.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// Имя поля проекта, к которому относится проблема.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Текст проблемы для пользователя. На русском.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Создать проблему валидации.
    /// </summary>
    /// <param name="severity">Уровень важности.</param>
    /// <param name="rowIndex">Индекс строки или <see cref="NoRow"/>.</param>
    /// <param name="fieldName">Имя поля проекта.</param>
    /// <param name="message">Текст для пользователя.</param>
    public ValidationIssue(
        IssueSeverity severity,
        int rowIndex,
        string fieldName,
        string message)
    {
        Severity = severity;
        RowIndex = rowIndex;
        FieldName = fieldName;
        Message = message;
    }
}