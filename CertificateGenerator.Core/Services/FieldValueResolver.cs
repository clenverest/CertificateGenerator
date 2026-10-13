using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Services;

/// <summary>
/// Вычисляет значение поля для конкретной строки таблицы.
/// Склеивает ФИО, берёт ячейку для обычного поля,
/// возвращает статическое значение для статического.
/// Не знает про UI — чистая логика.
/// </summary>
public static class FieldValueResolver
{
    /// <summary>
    /// Получить значение поля для строки таблицы.
    /// Никогда не возвращает <c>null</c>: если данных нет — пустая строка.
    /// </summary>
    /// <param name="field">Поле проекта.</param>
    /// <param name="row">Ячейки одной строки таблицы.</param>
    public static string Resolve(Field field, IReadOnlyList<string> row)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(row);

        return field.Kind switch
        {
            FieldKind.Simple => ResolveSimple(field, row),
            FieldKind.FullName => ResolveFullName(field, row),
            FieldKind.Static => ResolveStatic(field),
            _ => string.Empty
        };
    }

    // ---------------------------------------------------------------------
    // Simple
    // ---------------------------------------------------------------------

    private static string ResolveSimple(Field field, IReadOnlyList<string> row)
    {
        // Модель гарантирует ровно один индекс для Simple.
        var index = field.SourceColumnIndexes[0];

        if (index < 0 || index >= row.Count)
        {
            return string.Empty;
        }

        return Normalize(row[index]);
    }

    // ---------------------------------------------------------------------
    // FullName
    // ---------------------------------------------------------------------

    private static string ResolveFullName(Field field, IReadOnlyList<string> row)
    {
        var parts = new List<string>();

        foreach (var index in field.SourceColumnIndexes)
        {
            if (index < 0 || index >= row.Count)
            {
                continue;
            }

            var value = Normalize(row[index]);
            if (value.Length > 0)
            {
                parts.Add(value);
            }
        }

        return string.Join(field.Separator, parts);
    }

    // ---------------------------------------------------------------------
    // Static
    // ---------------------------------------------------------------------

    private static string ResolveStatic(Field field)
    {
        // StaticValue не null для Static — гарантия модели.
        return field.StaticValue ?? string.Empty;
    }

    // ---------------------------------------------------------------------
    // Утилита
    // ---------------------------------------------------------------------

    /// <summary>
    /// Обрезать пробелы по краям. <c>null</c> превратить в пустую строку.
    /// </summary>
    private static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}