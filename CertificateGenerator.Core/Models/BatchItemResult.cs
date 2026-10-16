namespace CertificateGenerator.Core.Models;

/// <summary>
/// Результат обработки одного участника.
/// </summary>
public sealed class BatchItemResult
{
    /// <summary>Индекс строки в таблице (с нуля).</summary>
    public int RowIndex { get; }

    /// <summary>ФИО или имя для отображения в отчёте.</summary>
    public string DisplayName { get; }

    /// <summary>Статус: успех, пропуск, ошибка.</summary>
    public BatchItemStatus Status { get; }

    /// <summary>Имя файла внутри ZIP. null, если статус != Success.</summary>
    public string? FileName { get; }

    /// <summary>Сообщение об ошибке или причина пропуска. null для Success.</summary>
    public string? Message { get; }

    public BatchItemResult(
        int rowIndex,
        string displayName,
        BatchItemStatus status,
        string? fileName = null,
        string? message = null)
    {
        RowIndex = rowIndex;
        DisplayName = displayName ?? string.Empty;
        Status = status;
        FileName = fileName;
        Message = message;
    }
}