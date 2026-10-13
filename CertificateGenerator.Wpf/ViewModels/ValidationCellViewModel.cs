namespace CertificateGenerator.Wpf.ViewModels;

/// <summary>
/// ViewModel одной ячейки в таблице проверки.
/// Хранит вычисленное значение и флаги проблем.
/// </summary>
public sealed class ValidationCellViewModel
{
    /// <summary>
    /// Имя поля проекта, к которому относится ячейка.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Вычисленное значение поля для этой строки.
    /// Никогда не <c>null</c>; пустое значение — пустая строка.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Есть ли в ячейке ошибка (Severity = Error).
    /// </summary>
    public bool HasError { get; }

    /// <summary>
    /// Есть ли в ячейке предупреждение (Severity = Warning).
    /// </summary>
    public bool HasWarning { get; }

    /// <summary>
    /// Пустое ли значение ячейки. Используется для показа «— пусто —».
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    /// <summary>
    /// Создать ячейку.
    /// </summary>
    public ValidationCellViewModel(
        string fieldName,
        string value,
        bool hasError,
        bool hasWarning)
    {
        FieldName = fieldName ?? string.Empty;
        Value = value ?? string.Empty;
        HasError = hasError;
        HasWarning = hasWarning;
    }
}