using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Interfaces;

/// <summary>
/// Хранилище проектов: загрузка, сохранение, создание, проверка существования.
/// </summary>
public interface IProjectStorage
{
    /// <summary>
    /// Создать новый проект в указанной папке.
    /// Папка создаётся, если её нет. Внутри создаётся структура проекта
    /// и файл с настройками.
    /// </summary>
    /// <param name="projectFolderPath">Путь к папке проекта.</param>
    /// <param name="projectName">Имя проекта.</param>
    /// <returns>Только что созданный проект со значениями по умолчанию.</returns>
    Project Create(string projectFolderPath, string projectName);

    /// <summary>
    /// Загрузить проект из указанной папки.
    /// </summary>
    /// <param name="projectFolderPath">Путь к папке проекта.</param>
    /// <returns>Загруженный проект.</returns>
    Project Load(string projectFolderPath);

    /// <summary>
    /// Сохранить проект в указанную папку.
    /// </summary>
    /// <param name="project">Проект для сохранения.</param>
    /// <param name="projectFolderPath">Путь к папке проекта.</param>
    void Save(Project project, string projectFolderPath);

    /// <summary>
    /// Проверить, является ли указанная папка папкой проекта.
    /// </summary>
    /// <param name="projectFolderPath">Путь к папке.</param>
    /// <returns>true, если в папке есть проект.</returns>
    bool Exists(string projectFolderPath);
}