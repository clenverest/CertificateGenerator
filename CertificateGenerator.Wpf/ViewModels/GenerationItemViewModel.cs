using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Wpf.ViewModels;

/// <summary>
/// Одна строка в списке результатов массовой генерации.
/// </summary>
public sealed class GenerationItemViewModel
{
    public int RowIndex { get; }
    public string DisplayName { get; }
    public BatchItemStatus Status { get; }
    public string? FileName { get; }
    public string? Message { get; }

    /// <summary>Номер строки для отображения (с 1).</summary>
    public int DisplayRowNumber => RowIndex + 1;

    /// <summary>Статус в виде текста для UI.</summary>
    public string StatusLabel => Status switch
    {
        BatchItemStatus.Success => "✓ Успех",
        BatchItemStatus.Skipped => "⚠ Пропущено",
        BatchItemStatus.Failed => "✗ Ошибка",
        _ => "?"
    };

    /// <summary>Цвет статуса.</summary>
    public string StatusColor => Status switch
    {
        BatchItemStatus.Success => "#107C10",   // зелёный
        BatchItemStatus.Skipped => "#8A6D00",   // жёлтый
        BatchItemStatus.Failed => "#C42B1C",    // красный
        _ => "#605E5C"
    };

    /// <summary>Что показать в колонке «Подробности».</summary>
    public string Details => Status switch
    {
        BatchItemStatus.Success => FileName ?? string.Empty,
        BatchItemStatus.Skipped => Message ?? "Пропущено",
        BatchItemStatus.Failed => Message ?? "Ошибка",
        _ => string.Empty
    };

    public GenerationItemViewModel(BatchItemResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        RowIndex = result.RowIndex;
        DisplayName = result.DisplayName;
        Status = result.Status;
        FileName = result.FileName;
        Message = result.Message;
    }
}