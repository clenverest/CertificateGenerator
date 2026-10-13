using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Services;

/// <summary>
/// Подсказывает, какие колонки таблицы подходят для поля,
/// на основе имени поля и имён колонок.
/// Не знает про UI — чистая логика.
/// </summary>
public static class ColumnSuggester
{
    /// <summary>
    /// Подсказать колонку-источник для обычного поля.
    /// Возвращает пустой список, если однозначного совпадения нет.
    /// </summary>
    public static IReadOnlyList<int> SuggestSimple(
        string fieldName,
        IReadOnlyList<ColumnInfo> columns)
    {
        if (string.IsNullOrWhiteSpace(fieldName) || columns.Count == 0)
        {
            return Array.Empty<int>();
        }

        var normalizedField = Normalize(fieldName);

        var matches = columns
            .Where(c => Normalize(c.Name) == normalizedField)
            .ToList();

        // Однозначное совпадение — ровно одна колонка.
        if (matches.Count == 1)
        {
            return new[] { matches[0].Index };
        }

        return Array.Empty<int>();
    }

    /// <summary>
    /// Подсказать колонки-источники для поля ФИО.
    /// Сначала ищет одну колонку «ФИО». Если не нашёл —
    /// ищет три колонки: фамилия, имя, отчество.
    /// Возвращает пустой список, если ничего однозначного нет.
    /// </summary>
    public static IReadOnlyList<int> SuggestFullName(
        IReadOnlyList<ColumnInfo> columns)
    {
        if (columns.Count == 0)
        {
            return Array.Empty<int>();
        }

        // Шаг 1: одна колонка «ФИО» (любой регистр, с «ё»).
        var fullNameMatches = columns
            .Where(c => Normalize(c.Name) == "фио")
            .ToList();

        if (fullNameMatches.Count == 1)
        {
            return new[] { fullNameMatches[0].Index };
        }

        // Шаг 2: три колонки — фамилия, имя, отчество.
        var surname = FindSingleMatch(columns, SurnameVariants);
        var firstName = FindSingleMatch(columns, FirstNameVariants);
        var middleName = FindSingleMatch(columns, MiddleNameVariants);

        if (surname is not null && firstName is not null && middleName is not null)
        {
            return new[] { surname.Value, firstName.Value, middleName.Value };
        }

        return Array.Empty<int>();
    }

    // ---------------------------------------------------------------------
    // Приватное
    // ---------------------------------------------------------------------

    private static readonly string[] SurnameVariants =
    {
        "фамилия",
        "фамилии",
        "фамилия ученика",
        "фамилия участника",
        "surname",
        "last name",
    };

    private static readonly string[] FirstNameVariants =
    {
        "имя",
        "имена",
        "имя ученика",
        "имя участника",
        "name",
        "first name",
    };

    private static readonly string[] MiddleNameVariants =
    {
        "отчество",
        "отчества",
        "отчество ученика",
        "отчество участника",
        "middle name",
    };

    /// <summary>
    /// Найти колонку, имя которой совпадает ровно с одним из вариантов.
    /// Если совпадений несколько (в т.ч. по разным вариантам) — вернуть null.
    /// </summary>
    private static int? FindSingleMatch(
        IReadOnlyList<ColumnInfo> columns,
        IReadOnlyList<string> variants)
    {
        var matches = columns
            .Where(c => variants.Contains(Normalize(c.Name)))
            .ToList();

        return matches.Count == 1 ? matches[0].Index : null;
    }

    /// <summary>
    /// Нормализовать имя: нижний регистр, ё→е, обрезать пробелы,
    /// схлопнуть внутренние пробелы в один.
    /// </summary>
    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var lower = value.Trim().ToLowerInvariant();
        var replaced = lower.Replace('ё', 'е');

        // Схлопываем внутренние пробелы: "имя  ученика" → "имя ученика".
        var parts = replaced.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);

        return string.Join(' ', parts);
    }
}