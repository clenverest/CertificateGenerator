using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Interfaces;

/// <summary>
/// Сервис показа диалога создания нового проекта.
/// </summary>
public interface ICreateProjectDialogService
{
    /// <summary>
    /// Показать диалог создания проекта.
    /// </summary>
    /// <returns>
    /// Результат с путём и именем, или null, если пользователь отменил.
    /// </returns>
    CreateProjectResult? Show();
}