namespace CertificateGenerator.Core.Models;

/// <summary>
/// Поле проекта: логическое поле, которое будет заполняться данными
/// из одной или нескольких колонок таблицы, либо статическим значением.
/// </summary>
public sealed class Field
{
    /// <summary>
    /// Отображаемое имя поля («ФИО», «Класс», «Школа»).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Тип поля — обычное, ФИО-склейка или статическое.
    /// </summary>
    public FieldKind Kind { get; }

    /// <summary>
    /// Индексы колонок таблицы-источника.
    /// Для <see cref="FieldKind.Simple"/> — ровно один.
    /// Для <see cref="FieldKind.FullName"/> — от 1 до 3.
    /// Для <see cref="FieldKind.Static"/> — пустой список.
    /// </summary>
    public IReadOnlyList<int> SourceColumnIndexes { get; }

    /// <summary>
    /// Значение для <see cref="FieldKind.Static"/>.
    /// Для остальных типов — <c>null</c>.
    /// </summary>
    public string? StaticValue { get; }

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
    /// <param name="sourceColumnIndexes">Индексы колонок-источников.
    /// Для <see cref="FieldKind.Static"/> должен быть пустым.</param>
    /// <param name="isRequired">Обязательное ли поле.</param>
    /// <param name="separator">Разделитель для склейки. По умолчанию — пробел.</param>
    /// <param name="staticValue">Значение для <see cref="FieldKind.Static"/>.
    /// Для остальных типов должен быть <c>null</c>.</param>
    /// <exception cref="ArgumentException">
    /// Бросается, если имя пустое, индексы некорректны для выбранного типа,
    /// среди индексов есть отрицательные, либо <paramref name="staticValue"/>
    /// не согласован с <paramref name="kind"/>.
    /// </exception>
    public Field(
        string name,
        FieldKind kind,
        IReadOnlyList<int> sourceColumnIndexes,
        bool isRequired = false,
        string separator = " ",
        string? staticValue = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Имя поля не может быть пустым.", nameof(name));
        }

        if (sourceColumnIndexes is null)
        {
            throw new ArgumentNullException(nameof(sourceColumnIndexes));
        }

        if (sourceColumnIndexes.Any(index => index < 0))
        {
            throw new ArgumentException(
                "Индексы колонок не могут быть отрицательными.",
                nameof(sourceColumnIndexes));
        }

        switch (kind)
        {
            case FieldKind.Simple:
                if (sourceColumnIndexes.Count != 1)
                {
                    throw new ArgumentException(
                        "У обычного поля должна быть ровно одна колонка-источник.",
                        nameof(sourceColumnIndexes));
                }
                if (staticValue is not null)
                {
                    throw new ArgumentException(
                        "StaticValue задаётся только для статических полей.",
                        nameof(staticValue));
                }
                break;

            case FieldKind.FullName:
                if (sourceColumnIndexes.Count < 1 || sourceColumnIndexes.Count > 3)
                {
                    throw new ArgumentException(
                        "Поле ФИО может склеиваться из 1–3 колонок.",
                        nameof(sourceColumnIndexes));
                }
                if (staticValue is not null)
                {
                    throw new ArgumentException(
                        "StaticValue задаётся только для статических полей.",
                        nameof(staticValue));
                }
                break;

            case FieldKind.Static:
                if (sourceColumnIndexes.Count != 0)
                {
                    throw new ArgumentException(
                        "У статического поля не должно быть колонок-источников.",
                        nameof(sourceColumnIndexes));
                }
                if (string.IsNullOrWhiteSpace(staticValue))
                {
                    throw new ArgumentException(
                        "У статического поля должно быть непустое значение.",
                        nameof(staticValue));
                }
                break;

            default:
                throw new ArgumentException(
                    $"Неизвестный тип поля: {kind}.",
                    nameof(kind));
        }

        Name = name;
        Kind = kind;
        SourceColumnIndexes = sourceColumnIndexes;
        IsRequired = isRequired;
        Separator = separator;
        StaticValue = staticValue;
    }

    /// <summary>
    /// Проверить имя поля. Возвращает <c>null</c>, если имя корректно,
    /// иначе — сообщение об ошибке на русском.
    /// </summary>
    /// <param name="name">Имя для проверки.</param>
    /// <param name="existingNames">Имена уже существующих полей
    /// (для проверки на дубликат, регистронезависимо).</param>
    public static string? ValidateName(string? name, IReadOnlyList<string> existingNames)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Имя поля не может быть пустым.";
        }

        var trimmed = name.Trim();

        foreach (var existing in existingNames)
        {
            if (string.Equals(existing, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return $"Поле с именем \"{trimmed}\" уже существует.";
            }
        }

        return null;
    }
}