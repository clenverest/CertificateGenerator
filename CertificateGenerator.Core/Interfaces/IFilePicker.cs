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

    /// <summary>
    /// Показать диалог сохранения файла.
    /// </summary>
    /// <param name="title">Заголовок диалога.</param>
    /// <param name="filter">
    /// Фильтр в формате WPF: "PDF-документы (*.pdf)|*.pdf".
    /// </param>
    /// <param name="defaultFileName">Имя файла по умолчанию, без пути.</param>
    /// <param name="defaultFolderPath">
    /// Папка, в которой открыть диалог. Может быть null — тогда
    /// используется системная папка по умолчанию.
    /// </param>
    /// <returns>
    /// Путь к выбранному файлу, или null, если пользователь отменил.
    /// </returns>
    string? PickSaveFile(
        string title,
        string filter,
        string defaultFileName,
        string? defaultFolderPath = null);
}