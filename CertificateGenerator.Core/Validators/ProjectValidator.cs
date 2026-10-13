using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Validators;

/// <summary>
/// Агрегатор валидаторов: запускает все дочерние валидаторы
/// и объединяет найденные проблемы в один список.
/// </summary>
public sealed class ProjectValidator : IValidator
{
    private readonly IReadOnlyList<IValidator> _validators;

    /// <summary>
    /// Создать агрегатор с заданным набором валидаторов.
    /// </summary>
    /// <param name="validators">Валидаторы, которые надо запускать.
    /// Может быть пустым — тогда результат всегда пустой.</param>
    public ProjectValidator(IReadOnlyList<IValidator> validators)
    {
        ArgumentNullException.ThrowIfNull(validators);
        _validators = validators;
    }

    /// <summary>
    /// Создать агрегатор со стандартным набором валидаторов:
    /// обязательные поля, дубликаты, лишние пробелы.
    /// </summary>
    public static ProjectValidator CreateDefault()
    {
        return new ProjectValidator(new IValidator[]
        {
            new RequiredFieldValidator(),
            new DuplicateValidator(),
            new WhitespaceValidator()
        });
    }

    /// <summary>
    /// Запустить все валидаторы и объединить результаты.
    /// </summary>
    public IReadOnlyList<ValidationIssue> Validate(
        TableData table,
        IReadOnlyList<Field> fields)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(fields);

        if (_validators.Count == 0)
        {
            return Array.Empty<ValidationIssue>();
        }

        var result = new List<ValidationIssue>();

        foreach (var validator in _validators)
        {
            var issues = validator.Validate(table, fields);
            result.AddRange(issues);
        }

        return result;
    }
}