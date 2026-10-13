using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using CertificateGenerator.Core.Services;

namespace CertificateGenerator.Core.Validators;

/// <summary>
/// Проверяет, что обязательные поля заполнены во всех строках.
/// </summary>
public sealed class RequiredFieldValidator : IValidator
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

        var requiredFields = fields.Where(f => f.IsRequired).ToList();
        if (requiredFields.Count == 0)
        {
            return issues;
        }

        for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
        {
            var row = table.Rows[rowIndex];

            foreach (var field in requiredFields)
            {
                var value = FieldValueResolver.Resolve(field, row);
                if (string.IsNullOrEmpty(value))
                {
                    issues.Add(new ValidationIssue(
                        IssueSeverity.Error,
                        rowIndex,
                        field.Name,
                        $"Поле \"{field.Name}\" обязательно для заполнения."));
                }
            }
        }

        return issues;
    }
}