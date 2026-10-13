using CertificateGenerator.Core.Models;
using CertificateGenerator.Core.Validators;

namespace CertificateGenerator.Tests.Validators;

public class WhitespaceValidatorTests
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
            new[] { "  Иванов  " });

        var result = new WhitespaceValidator().Validate(
            table, Array.Empty<Field>());

        Assert.Empty(result);
    }

    [Fact]
    public void EmptyTable_ReturnsEmpty()
    {
        var table = Table(new[] { Col(0, "ФИО") });

        var field = new Field("ФИО", FieldKind.Simple, new[] { 0 });

        var result = new WhitespaceValidator().Validate(
            table, new[] { field });

        Assert.Empty(result);
    }

    // ---------------------------------------------------------------------
    // Simple
    // ---------------------------------------------------------------------

    [Fact]
    public void Simple_CleanValue_ReturnsEmpty()
    {
        var table = Table(
            new[] { Col(0, "Класс") },
            new[] { "5А" });

        var field = new Field("Класс", FieldKind.Simple, new[] { 0 });

        var result = new WhitespaceValidator().Validate(
            table, new[] { field });

        Assert.Empty(result);
    }

    [Fact]
    public void Simple_LeadingWhitespace_ReturnsWarning()
    {
        var table = Table(
            new[] { Col(0, "Класс") },
            new[] { "  5А" });

        var field = new Field("Класс", FieldKind.Simple, new[] { 0 });

        var result = new WhitespaceValidator().Validate(
            table, new[] { field });

        Assert.Single(result);
        Assert.Equal(IssueSeverity.Warning, result[0].Severity);
        Assert.Equal(0, result[0].RowIndex);
        Assert.Equal("Класс", result[0].FieldName);
    }

    [Fact]
    public void Simple_TrailingWhitespace_ReturnsWarning()
    {
        var table = Table(
            new[] { Col(0, "Класс") },
            new[] { "5А  " });

        var field = new Field("Класс", FieldKind.Simple, new[] { 0 });

        var result = new WhitespaceValidator().Validate(
            table, new[] { field });

        Assert.Single(result);
    }

    [Fact]
    public void Simple_BothSides_ReturnsOneWarning()
    {
        var table = Table(
            new[] { Col(0, "Класс") },
            new[] { "  5А  " });

        var field = new Field("Класс", FieldKind.Simple, new[] { 0 });

        var result = new WhitespaceValidator().Validate(
            table, new[] { field });

        // Одна проблема на одну ячейку, даже если пробелы с двух сторон.
        Assert.Single(result);
    }

    [Fact]
    public void Simple_InternalSpaces_NotAProblem()
    {
        var table = Table(
            new[] { Col(0, "ФИО") },
            new[] { "Иван  Иванович" });  // два пробела внутри

        var field = new Field("ФИО", FieldKind.Simple, new[] { 0 });

        var result = new WhitespaceValidator().Validate(
            table, new[] { field });

        Assert.Empty(result);
    }

    [Fact]
    public void Simple_OnlySpaces_TreatedAsEmpty()
    {
        var table = Table(
            new[] { Col(0, "Класс") },
            new[] { "   " });

        var field = new Field("Класс", FieldKind.Simple, new[] { 0 });

        var result = new WhitespaceValidator().Validate(
            table, new[] { field });

        // Не «лишние пробелы», а «пусто». Проверит RequiredFieldValidator.
        Assert.Empty(result);
    }

    [Fact]
    public void Simple_EmptyValue_ReturnsEmpty()
    {
        var table = Table(
            new[] { Col(0, "Класс") },
            new[] { "" });

        var field = new Field("Класс", FieldKind.Simple, new[] { 0 });

        var result = new WhitespaceValidator().Validate(
            table, new[] { field });

        Assert.Empty(result);
    }

    // ---------------------------------------------------------------------
    // FullName
    // ---------------------------------------------------------------------

    [Fact]
    public void FullName_AllClean_ReturnsEmpty()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя"), Col(2, "Отчество") },
            new[] { "Иванов", "Иван", "Иванович" });

        var field = new Field("ФИО", FieldKind.FullName, new[] { 0, 1, 2 });

        var result = new WhitespaceValidator().Validate(
            table, new[] { field });

        Assert.Empty(result);
    }

    [Fact]
    public void FullName_OneColumnWithSpaces_ReturnsOneWarning()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя") },
            new[] { "  Иванов  ", "Иван" });

        var field = new Field("ФИО", FieldKind.FullName, new[] { 0, 1 });

        var result = new WhitespaceValidator().Validate(
            table, new[] { field });

        Assert.Single(result);
        Assert.Equal("ФИО", result[0].FieldName);
        Assert.Contains("Фамилия", result[0].Message);
    }

    [Fact]
    public void FullName_TwoColumnsWithSpaces_ReturnsTwoWarnings()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя") },
            new[] { "  Иванов", "  Иван" });

        var field = new Field("ФИО", FieldKind.FullName, new[] { 0, 1 });

        var result = new WhitespaceValidator().Validate(
            table, new[] { field });

        Assert.Equal(2, result.Count);
        // Обе — на строку 0.
        Assert.All(result, i => Assert.Equal(0, i.RowIndex));
    }

    // ---------------------------------------------------------------------
    // Static — пропускается
    // ---------------------------------------------------------------------

    [Fact]
    public void Static_IsSkipped()
    {
        var table = Table(
            new[] { Col(0, "ФИО") },
            new[] { "  Иванов  " });

        var field = new Field(
            "Дата", FieldKind.Static, Array.Empty<int>(),
            staticValue: "  1 сентября  ");  // специально с пробелами

        var result = new WhitespaceValidator().Validate(
            table, new[] { field });

        Assert.Empty(result);
    }

    // ---------------------------------------------------------------------
    // Несколько строк и полей
    // ---------------------------------------------------------------------

    [Fact]
    public void MultipleRows_ReportsEachRow()
    {
        var table = Table(
            new[] { Col(0, "Класс") },
            new[] { "  5А  " },
            new[] { "6Б" },
            new[] { "  7В" });

        var field = new Field("Класс", FieldKind.Simple, new[] { 0 });

        var result = new WhitespaceValidator().Validate(
            table, new[] { field });

        Assert.Equal(2, result.Count);
        Assert.Equal(0, result[0].RowIndex);
        Assert.Equal(2, result[1].RowIndex);
    }

    [Fact]
    public void MultipleFields_ReportsEachField()
    {
        var table = Table(
            new[] { Col(0, "ФИО"), Col(1, "Класс") },
            new[] { "  Иванов", "  5А" });

        var fio = new Field("ФИО", FieldKind.Simple, new[] { 0 });
        var klass = new Field("Класс", FieldKind.Simple, new[] { 1 });

        var result = new WhitespaceValidator().Validate(
            table, new[] { fio, klass });

        Assert.Equal(2, result.Count);
        Assert.Equal("ФИО", result[0].FieldName);
        Assert.Equal("Класс", result[1].FieldName);
    }
}