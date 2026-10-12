using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Interfaces;

/// <summary>
/// Хранилище списка последних открытых проектов.
/// </summary>
public interface IRecentProjectsStore
{
    /// <summary>
    /// Получить список последних проектов, от свежих к старым.
    /// </summary>
    IReadOnlyList<RecentProject> GetAll();

    /// <summary>
    /// Добавить проект в список. Если запись с таким путём уже есть —
    /// обновляет её дату, имя и перемещает в начало списка.
    /// Если записей больше лимита — самые старые отбрасываются.
    /// </summary>
    /// <param name="projectFolderPath">Путь к папке проекта.</param>
    /// <param name="projectName">Имя проекта.</param>
    void Add(string projectFolderPath, string projectName);

    /// <summary>
    /// Удалить запись о проекте по пути. Если записи нет — ничего не делает.
    /// </summary>
    /// <param name="projectFolderPath">Путь к папке проекта.</param>
    void Remove(string projectFolderPath);

    /// <summary>
    /// Очистить весь список.
    /// </summary>
    void Clear();
}