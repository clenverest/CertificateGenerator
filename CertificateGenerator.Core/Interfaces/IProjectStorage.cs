using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Interfaces;

public interface IProjectStorage
{
    Project Create(string projectFolderPath, string projectName);

    Project Load(string projectFolderPath);

    void Save(Project project, string projectFolderPath);

    bool Exists(string projectFolderPath);

    /// <summary>
    /// Скопировать файл таблицы в папку source/ проекта
    /// и обновить project.json.
    /// </summary>
    /// <param name="projectFolderPath">Путь к папке проекта.</param>
    /// <param name="project">Текущий проект. Используется для Name, CreatedAt, Fields.</param>
    /// <param name="sourceFilePath">Путь к исходному файлу таблицы.</param>
    /// <returns>Новый Project с обновлённым SourceFileName.</returns>
    Project SetSourceTable(
        string projectFolderPath,
        Project project,
        string sourceFilePath);

    /// <summary>
    /// Получить полный путь к файлу таблицы внутри проекта.
    /// </summary>
    /// <returns>
    /// Путь к файлу, если SourceFileName не пуст и файл существует.
    /// null — если таблица не загружена или файл отсутствует.
    /// </returns>
    string? GetSourceTablePath(Project project, string projectFolderPath);
}