namespace CertificateGenerator.Core.Models;

/// <summary>
/// Снимок прогресса массовой генерации.
/// </summary>
public sealed class BatchProgress
{
    /// <summary>Сколько обработано (включая пропущенных и ошибочных).</summary>
    public int Processed { get; }

    /// <summary>Сколько всего.</summary>
    public int Total { get; }

    /// <summary>Имя текущего обрабатываемого участника.</summary>
    public string CurrentName { get; }

    /// <summary>Прогресс в диапазоне 0..1.</summary>
    public double Fraction => Total > 0 ? (double)Processed / Total : 0;

    public BatchProgress(int processed, int total, string currentName)
    {
        Processed = processed;
        Total = total;
        CurrentName = currentName ?? string.Empty;
    }
}