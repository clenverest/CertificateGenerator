namespace CertificateGenerator.Core.Models;

/// <summary>
/// Итоговый отчёт массовой генерации.
/// </summary>
public sealed class BatchReport
{
    /// <summary>Все результаты — по одному на каждую строку.</summary>
    public IReadOnlyList<BatchItemResult> Items { get; }

    /// <summary>Путь к созданному ZIP-архиву. null, если не создан.</summary>
    public string? ZipPath { get; }

    /// <summary>Количество успешных.</summary>
    public int SuccessCount => Items.Count(i => i.Status == BatchItemStatus.Success);

    /// <summary>Количество пропущенных.</summary>
    public int SkippedCount => Items.Count(i => i.Status == BatchItemStatus.Skipped);

    /// <summary>Количество с ошибками.</summary>
    public int FailedCount => Items.Count(i => i.Status == BatchItemStatus.Failed);

    /// <summary>Всего обработано.</summary>
    public int TotalCount => Items.Count;

    /// <summary>Была ли генерация отменена пользователем.</summary>
    public bool WasCancelled { get; }

    public BatchReport(
        IReadOnlyList<BatchItemResult> items,
        string? zipPath,
        bool wasCancelled)
    {
        Items = items ?? Array.Empty<BatchItemResult>();
        ZipPath = zipPath;
        WasCancelled = wasCancelled;
    }
}