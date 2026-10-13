using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Tests.Fakes;

/// <summary>
/// Фейковый валидатор: возвращает заранее заданный список проблем.
/// </summary>
public sealed class FakeValidator : IValidator
{
    public IReadOnlyList<ValidationIssue> IssuesToReturn { get; set; }
        = Array.Empty<ValidationIssue>();

    public List<(TableData Table, IReadOnlyList<Field> Fields)> Calls { get; } = new();

    public IReadOnlyList<ValidationIssue> Validate(
        TableData table,
        IReadOnlyList<Field> fields)
    {
        Calls.Add((table, fields));
        return IssuesToReturn;
    }
}