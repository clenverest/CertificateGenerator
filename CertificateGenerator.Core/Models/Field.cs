namespace CertificateGenerator.Core.Models;

/// <summary>
/// Поле проекта: логическое поле, которое будет заполняться данными
/// из одной или нескольких колонок таблицы.
/// </summary>
public sealed class Field
{
    /// <summary>
    /// Отображаемое имя поля («ФИО», «Класс», «Школа»).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Тип поля — обычное или ФИО-склейка.
    /// </summary>
    public FieldKind Kind { get; }

    /// <summary>
    /// Индексы колонок таблицы-источника.
    /// Для <see cref="FieldKind.Simple"/> — ровно один.
    /// Для <see cref="FieldKind.FullName"/> — от 1 до 3.
    /// </summary>
    public IReadOnlyList<int> SourceColumnIndexes { get; }

    /// <summary>
    /// Обязательное ли поле. Если да — пустое значение будет ошибкой валидации.
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// Разделитель для склейки (используется только для <see cref="FieldKind.FullName"/>).
    /// По умолчанию — один пробел.
    /// </summary>
    public string Separator { get; }

    /// <summary>
    /// Создать поле проекта.
    /// </summary>
    /// <param name="name">Отображаемое имя поля.</param>
    /// <param name="kind">Тип поля.</param>
    /// <param name="sourceColumnIndexes">Индексы колонок-источников.</param>
    /// <param name="isRequired">Обязательное ли поле.</param>
    /// <param name="separator">Разделитель для склейки. По умолчанию — пробел.</param>
    /// <exception cref="ArgumentException">
    /// Бросается, если имя пустое, индексы некорректны для выбранного типа
    /// или среди индексов есть отрицательные.
    /// </exception>
    public Field(
        string name,
        FieldKind kind,
        IReadOnlyList<int> sourceColumnIndexes,
        bool isRequired = false,
        string separator = " ")
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Имя поля не может быть пустым.", nameof(name));
        }

        if (sourceColumnIndexes is null)
        {
            throw new ArgumentNullException(nameof(sourceColumnIndexes));
        }

        if (sourceColumnIndexes.Count == 0)
        {
            throw new ArgumentException(
                "У поля должна быть хотя бы одна колонка-источник.",
                nameof(sourceColumnIndexes));
        }

        if (sourceColumnIndexes.Any(index => index < 0))
        {
            throw new ArgumentException(
                "Индексы колонок не могут быть отрицательными.",
                nameof(sourceColumnIndexes));
        }

        if (kind == FieldKind.Simple && sourceColumnIndexes.Count != 1)
        {
            throw new ArgumentException(
                "У обычного поля должна быть ровно одна колонка-источник.",
                nameof(sourceColumnIndexes));
        }

        if (kind == FieldKind.FullName && sourceColumnIndexes.Count > 3)
        {
            throw new ArgumentException(
                "Поле ФИО может склеиваться не более чем из трёх колонок.",
                nameof(sourceColumnIndexes));
        }

        Name = name;
        Kind = kind;
        SourceColumnIndexes = sourceColumnIndexes;
        IsRequired = isRequired;
        Separator = separator;
    }
}