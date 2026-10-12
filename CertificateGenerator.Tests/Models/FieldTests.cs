using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Tests.Models;

public sealed class FieldTests
{
    // --- Успешное создание ---

    [Fact]
    public void Constructor_SimpleFieldWithOneColumn_Succeeds()
    {
        var field = new Field("Класс", FieldKind.Simple, new[] { 3 });

        Assert.Equal("Класс", field.Name);
        Assert.Equal(FieldKind.Simple, field.Kind);
        Assert.Single(field.SourceColumnIndexes);
        Assert.Equal(3, field.SourceColumnIndexes[0]);
    }

    [Fact]
    public void Constructor_FullNameFieldWithOneColumn_Succeeds()
    {
        var field = new Field("ФИО", FieldKind.FullName, new[] { 0 });

        Assert.Equal(FieldKind.FullName, field.Kind);
        Assert.Single(field.SourceColumnIndexes);
    }

    [Fact]
    public void Constructor_FullNameFieldWithThreeColumns_Succeeds()
    {
        var field = new Field("ФИО", FieldKind.FullName, new[] { 0, 1, 2 });

        Assert.Equal(FieldKind.FullName, field.Kind);
        Assert.Equal(3, field.SourceColumnIndexes.Count);
    }

    // --- Значения по умолчанию ---

    [Fact]
    public void Constructor_DefaultValues_AreExpected()
    {
        var field = new Field("Класс", FieldKind.Simple, new[] { 1 });

        Assert.False(field.IsRequired);
        Assert.Equal(" ", field.Separator);
    }

    // --- Ошибки: некорректное число колонок ---

    [Fact]
    public void Constructor_SimpleFieldWithTwoColumns_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Field("Класс", FieldKind.Simple, new[] { 1, 2 }));
    }

    [Fact]
    public void Constructor_FullNameFieldWithFourColumns_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Field("ФИО", FieldKind.FullName, new[] { 0, 1, 2, 3 }));
    }

    [Fact]
    public void Constructor_FieldWithNoColumns_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Field("Класс", FieldKind.Simple, Array.Empty<int>()));
    }

    // --- Ошибки: некорректное имя ---

    [Fact]
    public void Constructor_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Field(string.Empty, FieldKind.Simple, new[] { 0 }));
    }
}