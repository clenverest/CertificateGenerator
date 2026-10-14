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
    /// Пустая строка, если таблица ещё не загружена.
    /// </summary>
    public string SourceFileName { get; }

    /// <summary>
    /// Список полей проекта.
    /// </summary>
    public IReadOnlyList<Field> Fields { get; }

    /// <summary>
    /// Правки строк таблицы. Пустой список, если правок нет.
    /// </summary>
    public IReadOnlyList<RowOverride> RowOverrides { get; }

    /// <summary>
    /// Информация о PNG-шаблоне. <c>null</c>, если шаблон ещё не загружен.
    /// </summary>
    public TemplateInfo? Template { get; }

    /// <summary>
    /// Размещения полей на шаблоне. Пустой список, если размещений нет.
    /// </summary>
    public IReadOnlyList<FieldPlacement> Placements { get; }

    /// <summary>
    /// Создать проект.
    /// </summary>
    /// <param name="name">Имя проекта.</param>
    /// <param name="createdAt">Дата и время создания.</param>
    /// <param name="sourceFileName">Имя исходного файла таблицы.
    /// Может быть пустой строкой, если таблица ещё не загружена.</param>
    /// <param name="fields">Список полей. Может быть пустым.</param>
    /// <param name="rowOverrides">Правки строк. Может быть <c>null</c> —
    /// тогда считается, что правок нет.</param>
    /// <param name="template">Информация о PNG-шаблоне.
    /// Может быть <c>null</c>, если шаблон ещё не загружен.</param>
    /// <param name="placements">Размещения полей на шаблоне.
    /// Может быть <c>null</c> — тогда считается, что размещений нет.</param>
    /// <exception cref="ArgumentException">
    /// Бросается, если имя проекта пусто.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Бросается, если <paramref name="sourceFileName"/>
    /// или <paramref name="fields"/> равны <c>null</c>.
    /// </exception>
    public Project(
        string name,
        DateTime createdAt,
        string sourceFileName,
        IReadOnlyList<Field> fields,
        IReadOnlyList<RowOverride>? rowOverrides = null,
        TemplateInfo? template = null,
        IReadOnlyList<FieldPlacement>? placements = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Имя проекта не может быть пустым.", nameof(name));
        }

        if (sourceFileName is null)
        {
            throw new ArgumentNullException(nameof(sourceFileName));
        }

        if (fields is null)
        {
            throw new ArgumentNullException(nameof(fields));
        }

        Name = name;
        CreatedAt = createdAt;
        SourceFileName = sourceFileName;
        Fields = fields;
        RowOverrides = rowOverrides?.ToArray() ?? Array.Empty<RowOverride>();
        Template = template;
        Placements = placements?.ToArray() ?? Array.Empty<FieldPlacement>();
    }
}