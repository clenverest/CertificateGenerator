namespace CertificateGenerator.Core.Models;

/// <summary>
/// Корневая модель проекта. Сериализуется в project.json.
/// </summary>
public sealed class Project
{
    /// <summary>
    /// Имя проекта. Отображается в UI.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Дата и время создания проекта.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Имя исходного файла таблицы внутри папки source/
    /// (например, "data.xlsx" или "data.csv").
    /// </summary>
    public string SourceFileName { get; }

    /// <summary>
    /// Список полей проекта.
    /// </summary>
    public IReadOnlyList<Field> Fields { get; }

    /// <summary>
    /// Создать проект.
    /// </summary>
    /// <param name="name">Имя проекта.</param>
    /// <param name="createdAt">Дата и время создания.</param>
    /// <param name="sourceFileName">Имя исходного файла таблицы.</param>
    /// <param name="fields">Список полей. Может быть пустым.</param>
    /// <exception cref="ArgumentException">
    /// Бросается, если имя проекта или имя исходного файла пусты.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Бросается, если <paramref name="fields"/> равен null.
    /// </exception>
    public Project(
        string name,
        DateTime createdAt,
        string sourceFileName,
        IReadOnlyList<Field> fields)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Имя проекта не может быть пустым.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(sourceFileName))
        {
            throw new ArgumentException(
                "Имя исходного файла не может быть пустым.",
                nameof(sourceFileName));
        }

        if (fields is null)
        {
            throw new ArgumentNullException(nameof(fields));
        }

        Name = name;
        CreatedAt = createdAt;
        SourceFileName = sourceFileName;
        Fields = fields;
    }
}