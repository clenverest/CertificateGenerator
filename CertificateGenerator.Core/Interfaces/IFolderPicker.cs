namespace CertificateGenerator.Core.Interfaces;

/// <summary>
/// Выбор папки пользователем через системный диалог.
/// </summary>
public interface IFolderPicker
{
    /// <summary>
    /// Показать диалог выбора папки.
    /// </summary>
    /// <param name="title">Заголовок диалога.</param>
    /// <returns>
    /// Путь к выбранной папке, или null, если пользователь отменил выбор.
    /// </returns>
    string? PickFolder(string title);
}