namespace CertificateGenerator.Core.Interfaces;

/// <summary>
/// Выбор файла пользователем через системный диалог.
/// </summary>
public interface IFilePicker
{
    /// <summary>
    /// Показать диалог выбора файла.
    /// </summary>
    /// <param name="title">Заголовок диалога.</param>
    /// <param name="filter">
    /// Фильтр в формате WPF: "Название|*.ext|Название2|*.ext2".
    /// Например: "PNG-картинки (*.png)|*.png".
    /// </param>
    /// <returns>
    /// Путь к выбранному файлу, или null, если пользователь отменил выбор.
    /// </returns>
    string? PickFile(string title, string filter);
}