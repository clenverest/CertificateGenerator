namespace CertificateGenerator.Core.Models;

/// <summary>
/// Статус обработки одного участника при массовой генерации.
/// </summary>
public enum BatchItemStatus
{
    /// <summary>Грамота сгенерирована и добавлена в ZIP.</summary>
    Success,

    /// <summary>Участник пропущен: нет данных для имени файла.</summary>
    Skipped,

    /// <summary>Ошибка при генерации (PDF или запись в ZIP).</summary>
    Failed
}