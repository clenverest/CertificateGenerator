using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Tests.Fakes;

/// <summary>
/// Фейковый сервис диалога поля: возвращает заранее заданные результаты.
/// </summary>
public sealed class FakeFieldEditDialogService : IFieldEditDialogService
{
    public Field? FieldToReturnOnCreate { get; set; }
    public Field? FieldToReturnOnEdit { get; set; }

    public List<(IReadOnlyList<string> ExistingNames, IReadOnlyList<ColumnInfo> Columns)>
        ShowCreateCalls { get; } = new();

    public List<(Field Field, IReadOnlyList<string> OtherNames, IReadOnlyList<ColumnInfo> Columns)>
        ShowEditCalls { get; } = new();

    public Field? ShowCreate(
        IReadOnlyList<string> existingNames,
        IReadOnlyList<ColumnInfo> columns)
    {
        ShowCreateCalls.Add((existingNames, columns));
        return FieldToReturnOnCreate;
    }

    public Field? ShowEdit(
        Field field,
        IReadOnlyList<string> existingNames,
        IReadOnlyList<ColumnInfo> columns)
    {
        ShowEditCalls.Add((field, existingNames, columns));
        return FieldToReturnOnEdit;
    }
}