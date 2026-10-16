using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using CertificateGenerator.Core.Services;

namespace CertificateGenerator.Core.Validators;

/// <summary>
/// Находит дубликаты значений в обязательных полях проекта.
/// Проверяются только поля с <see cref="Field.IsRequired"/> = true —
/// пользователь сам решает, какие поля должны быть уникальными
/// (обычно это ФИО).
/// Статические поля пропускаются: их значение одинаково по определению.
/// Пустые значения не считаются дубликатами.
/// </summary>
public sealed class DuplicateValidator : IValidator
{
    /// <summary>
    /// Провести валидацию.
    /// </summary>
    public IReadOnlyList<ValidationIssue> Validate(
        TableData table,
        IReadOnlyList<Field> fields)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(fields);

        var issues = new List<ValidationIssue>();

        if (table.Rows.Count < 2 || fields.Count == 0)
        {
            return issues;
        }

        foreach (var field in fields)
        {
            // Проверяем только обязательные поля (кроме Static —
            // у него значение одинаково по определению).
            if (field.Kind == FieldKind.Static || !field.IsRequired)
            {
                continue;
            }

            FindDuplicatesInField(field, table, issues);
        }

        return issues;
    }

    // ---------------------------------------------------------------------
    // Приватное
    // ---------------------------------------------------------------------

    private static void FindDuplicatesInField(
        Field field,
        TableData table,
        List<ValidationIssue> issues)
    {
        // Проход 1: собрать, где какие нормализованные значения встречаются.
        var rowIndexesByValue = new Dictionary<string, List<int>>();

        for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
        {
            var value = FieldValueResolver.Resolve(field, table.Rows[rowIndex]);
            var normalized = Normalize(value);

            if (normalized.Length == 0)
            {
                continue;
            }

            if (!rowIndexesByValue.TryGetValue(normalized, out var indexes))
            {
                indexes = new List<int>();
                rowIndexesByValue[normalized] = indexes;
            }
            indexes.Add(rowIndex);
        }

        // Дубликаты — значения, встретившиеся 2+ раза.
        var duplicateValues = rowIndexesByValue
            .Where(kv => kv.Value.Count >= 2)
            .Select(kv => kv.Key)
            .ToHashSet();

        if (duplicateValues.Count == 0)
        {
            return;
        }

        // Проход 2: пройти строки в порядке и добавить проблемы.
        for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
        {
            var value = FieldValueResolver.Resolve(field, table.Rows[rowIndex]);
            var normalized = Normalize(value);

            if (normalized.Length == 0 || !duplicateValues.Contains(normalized))
            {
                continue;
            }

            issues.Add(new ValidationIssue(
                IssueSeverity.Warning,
                rowIndex,
                field.Name,
                $"Значение поля \"{field.Name}\" повторяется: \"{value}\"."));
        }
    }

    /// <summary>
    /// Нормализовать значение для сравнения:
    /// обрезать пробелы, нижний регистр, ё→е.
    /// </summary>
    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().ToLowerInvariant().Replace('ё', 'е');
    }
}