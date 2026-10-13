using CertificateGenerator.Core.Models;
using CertificateGenerator.Core.Validators;

namespace CertificateGenerator.Tests.Validators;

public class RequiredFieldValidatorTests
{
    private static ColumnInfo Col(int index, string name) => new(index, name);

    private static TableData Table(
        IReadOnlyList<ColumnInfo> columns,
        params string[][] rows)
    {
        return new TableData(
            columns,
            rows.Select(r => (IReadOnlyList<string>)r).ToList());
    }

    // ---------------------------------------------------------------------
    // Нет работы — нет проблем
    // ---------------------------------------------------------------------

    [Fact]
    public void EmptyFields_ReturnsEmpty()
    {
        var table = Table(
            new[] { Col(0, "ФИО") },
            new[] { "Иванов" });

        var result = new RequiredFieldValidator().Validate(
            table, Array.Empty<Field>());

        Assert.Empty(result);
    }

    [Fact]
    public void EmptyTable_ReturnsEmpty()
    {
        var table = Table(new[] { Col(0, "ФИО") });

        var field = new Field(
            "ФИО", FieldKind.Simple, new[] { 0 }, isRequired: true);

        var result = new RequiredFieldValidator().Validate(
            table, new[] { field });

        Assert.Empty(result);
    }

    [Fact]
    public void NoRequiredFields_ReturnsEmpty()
    {
        var table = Table(
            new[] { Col(0, "ФИО") },
            new[] { "" });

        var field = new Field(
            "ФИО", FieldKind.Simple, new[] { 0 }, isRequired: false);

        var result = new RequiredFieldValidator().Validate(
            table, new[] { field });

        Assert.Empty(result);
    }

    // ---------------------------------------------------------------------
    // Simple
    // ---------------------------------------------------------------------

    [Fact]
    public void Simple_AllFilled_ReturnsEmpty()
    {
        var table = Table(
            new[] { Col(0, "Класс") },
            new[] { "5А" },
            new[] { "6Б" });

        var field = new Field(
            "Класс", FieldKind.Simple, new[] { 0 }, isRequired: true);

        var result = new RequiredFieldValidator().Validate(
            table, new[] { field });

        Assert.Empty(result);
    }

    [Fact]
    public void Simple_OneEmpty_ReturnsOneError()
    {
        var table = Table(
            new[] { Col(0, "Класс") },
            new[] { "5А" },
            new[] { "" });

        var field = new Field(
            "Класс", FieldKind.Simple, new[] { 0 }, isRequired: true);

        var result = new RequiredFieldValidator().Validate(
            table, new[] { field });

        Assert.Single(result);
        Assert.Equal(IssueSeverity.Error, result[0].Severity);
        Assert.Equal(1, result[0].RowIndex);
        Assert.Equal("Класс", result[0].FieldName);
    }

    [Fact]
    public void Simple_WhitespaceOnly_TreatedAsEmpty()
    {
        var table = Table(
            new[] { Col(0, "Класс") },
            new[] { "   " });

        var field = new Field(
            "Класс", FieldKind.Simple, new[] { 0 }, isRequired: true);

        var result = new RequiredFieldValidator().Validate(
            table, new[] { field });

        Assert.Single(result);
        Assert.Equal(0, result[0].RowIndex);
    }

    [Fact]
    public void Simple_AllEmpty_ReturnsOnePerRow()
    {
        var table = Table(
            new[] { Col(0, "Класс") },
            new[] { "" },
            new[] { "" },
            new[] { "" });

        var field = new Field(
            "Класс", FieldKind.Simple, new[] { 0 }, isRequired: true);

        var result = new RequiredFieldValidator().Validate(
            table, new[] { field });

        Assert.Equal(3, result.Count);
        Assert.Equal(0, result[0].RowIndex);
        Assert.Equal(1, result[1].RowIndex);
        Assert.Equal(2, result[2].RowIndex);
    }

    // ---------------------------------------------------------------------
    // FullName
    // ---------------------------------------------------------------------

    [Fact]
    public void FullName_AllParts_NoError()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя"), Col(2, "Отчество") },
            new[] { "Иванов", "Иван", "Иванович" });

        var field = new Field(
            "ФИО", FieldKind.FullName, new[] { 0, 1, 2 }, isRequired: true);

        var result = new RequiredFieldValidator().Validate(
            table, new[] { field });

        Assert.Empty(result);
    }

    [Fact]
    public void FullName_AllPartsEmpty_ReturnsError()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя"), Col(2, "Отчество") },
            new[] { "", "   ", "" });

        var field = new Field(
            "ФИО", FieldKind.FullName, new[] { 0, 1, 2 }, isRequired: true);

        var result = new RequiredFieldValidator().Validate(
            table, new[] { field });

        Assert.Single(result);
        Assert.Equal("ФИО", result[0].FieldName);
    }

    [Fact]
    public void FullName_OnePart_NoError()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя") },
            new[] { "Иванов", "" });

        var field = new Field(
            "ФИО", FieldKind.FullName, new[] { 0, 1 }, isRequired: true);

        var result = new RequiredFieldValidator().Validate(
            table, new[] { field });

        Assert.Empty(result);
    }

    // ---------------------------------------------------------------------
    // Static
    // ---------------------------------------------------------------------

    [Fact]
    public void Static_Required_NeverFails()
    {
        var table = Table(
            new[] { Col(0, "ФИО") },
            new[] { "Иванов" },
            new[] { "Петров" });

        var field = new Field(
            "Дата", FieldKind.Static, Array.Empty<int>(),
            isRequired: true,
            staticValue: "1 сентября 2026");

        var result = new RequiredFieldValidator().Validate(
            table, new[] { field });

        Assert.Empty(result);
    }

    // ---------------------------------------------------------------------
    // Несколько полей
    // ---------------------------------------------------------------------

    [Fact]
    public void MultipleFields_ReportsPerField()
    {
        var table = Table(
            new[] { Col(0, "ФИО"), Col(1, "Класс") },
            new[] { "", "" });

        var fio = new Field(
            "ФИО", FieldKind.Simple, new[] { 0 }, isRequired: true);
        var klass = new Field(
            "Класс", FieldKind.Simple, new[] { 1 }, isRequired: true);

        var result = new RequiredFieldValidator().Validate(
            table, new[] { fio, klass });

        Assert.Equal(2, result.Count);
        // Оба — на одну и ту же строку 0.
        Assert.All(result, i => Assert.Equal(0, i.RowIndex));
        Assert.Contains(result, i => i.FieldName == "ФИО");
        Assert.Contains(result, i => i.FieldName == "Класс");
    }

    [Fact]
    public void MixedRequiredAndOptional_OnlyRequiredChecked()
    {
        var table = Table(
            new[] { Col(0, "ФИО"), Col(1, "Комментарий") },
            new[] { "Иванов", "" });

        var fio = new Field(
            "ФИО", FieldKind.Simple, new[] { 0 }, isRequired: true);
        var comment = new Field(
            "Комментарий", FieldKind.Simple, new[] { 1 }, isRequired: false);

        var result = new RequiredFieldValidator().Validate(
            table, new[] { fio, comment });

        Assert.Empty(result);
    }
}