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

    public List<IReadOnlyList<string>> ShowCreateCalls { get; } = new();
    public List<(Field Field, IReadOnlyList<string> OtherNames)> ShowEditCalls { get; } = new();

    public Field? ShowCreate(IReadOnlyList<string> existingNames)
    {
        ShowCreateCalls.Add(existingNames);
        return FieldToReturnOnCreate;
    }

    public Field? ShowEdit(Field field, IReadOnlyList<string> existingNames)
    {
        ShowEditCalls.Add((field, existingNames));
        return FieldToReturnOnEdit;
    }
}