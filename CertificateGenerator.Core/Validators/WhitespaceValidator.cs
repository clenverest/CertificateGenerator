using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Validators;

/// <summary>
/// Находит ячейки, значения которых содержат лишние пробелы
/// по краям. Не использует FieldValueResolver, потому что тот тримит.
/// Статические поля не проверяются.
/// </summary>
public sealed class WhitespaceValidator : IValidator
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

        if (table.Rows.Count == 0 || fields.Count == 0)
        {
            return issues;
        }

        for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
        {
            var row = table.Rows[rowIndex];

            foreach (var field in fields)
            {
                if (field.Kind == FieldKind.Static)
                {
                    continue;
                }

                CheckField(field, table, row, rowIndex, issues);
            }
        }

        return issues;
    }

    // ---------------------------------------------------------------------
    // Приватное
    // ---------------------------------------------------------------------

    private static void CheckField(
        Field field,
        TableData table,
        IReadOnlyList<string> row,
        int rowIndex,
        List<ValidationIssue> issues)
    {
        foreach (var columnIndex in field.SourceColumnIndexes)
        {
            if (columnIndex < 0 || columnIndex >= row.Count)
            {
                continue;
            }

            var value = row[columnIndex];
            if (value is null)
            {
                continue;
            }

            if (value.Length == 0)
            {
                continue;
            }

            var trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                // Ячейка состоит только из пробелов — это «пусто», не «лишние пробелы».
                continue;
            }

            if (value.Length == trimmed.Length)
            {
                // Пробелов по краям нет.
                continue;
            }

            var columnName = GetColumnName(table, columnIndex);
            var message = field.Kind == FieldKind.Simple
                ? $"Значение поля \"{field.Name}\" содержит лишние пробелы."
                : $"Значение поля \"{field.Name}\" (колонка \"{columnName}\") содержит лишние пробелы.";

            issues.Add(new ValidationIssue(
                IssueSeverity.Warning,
                rowIndex,
                field.Name,
                message));
        }
    }

    private static string GetColumnName(TableData table, int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= table.Columns.Count)
        {
            return string.Empty;
        }

        return table.Columns[columnIndex].Name;
    }
}