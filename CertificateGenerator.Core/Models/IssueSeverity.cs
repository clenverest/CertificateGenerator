namespace CertificateGenerator.Core.Models;

/// <summary>
/// Уровень важности проблемы валидации.
/// </summary>
public enum IssueSeverity
{
    /// <summary>
    /// Предупреждение: пользователь может проигнорировать,
    /// но стоит обратить внимание.
    /// </summary>
    Warning = 0,

    /// <summary>
    /// Ошибка: генерацию грамоты проводить нельзя, пока не исправлено.
    /// </summary>
    Error = 1
}