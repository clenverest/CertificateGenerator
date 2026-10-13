using CertificateGenerator.Core.Models;
using CertificateGenerator.Core.Validators;

namespace CertificateGenerator.Tests.Validators;

public class DuplicateValidatorTests
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
            new[] { "Иванов" },
            new[] { "Иванов" });

        var result = new DuplicateValidator().Validate(
            table, Array.Empty<Field>());

        Assert.Empty(result);
    }

    [Fact]
    public void OneRow_ReturnsEmpty()
    {
        var table = Table(
            new[] { Col(0, "ФИО") },
            new[] { "Иванов" });

        var field = new Field("ФИО", FieldKind.Simple, new[] { 0 });

        var result = new DuplicateValidator().Validate(
            table, new[] { field });

        Assert.Empty(result);
    }

    [Fact]
    public void AllUnique_ReturnsEmpty()
    {
        var table = Table(
            new[] { Col(0, "ФИО") },
            new[] { "Иванов" },
            new[] { "Петров" },
            new[] { "Сидоров" });

        var field = new Field("ФИО", FieldKind.Simple, new[] { 0 });

        var result = new DuplicateValidator().Validate(
            table, new[] { field });

        Assert.Empty(result);
    }

    // ---------------------------------------------------------------------
    // Simple
    // ---------------------------------------------------------------------

    [Fact]
    public void Simple_TwoIdentical_ReturnsTwoWarnings()
    {
        var table = Table(
            new[] { Col(0, "ФИО") },
            new[] { "Иванов" },
            new[] { "Петров" },
            new[] { "Иванов" });

        var field = new Field("ФИО", FieldKind.Simple, new[] { 0 });

        var result = new DuplicateValidator().Validate(
            table, new[] { field });

        Assert.Equal(2, result.Count);
        Assert.All(result, i => Assert.Equal(IssueSeverity.Warning, i.Severity));
        Assert.All(result, i => Assert.Equal("ФИО", i.FieldName));
        Assert.Equal(0, result[0].RowIndex);
        Assert.Equal(2, result[1].RowIndex);
    }

    [Fact]
    public void Simple_ThreeIdentical_ReturnsThreeWarnings()
    {
        var table = Table(
            new[] { Col(0, "ФИО") },
            new[] { "Иванов" },
            new[] { "Иванов" },
            new[] { "Иванов" });

        var field = new Field("ФИО", FieldKind.Simple, new[] { 0 });

        var result = new DuplicateValidator().Validate(
            table, new[] { field });

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Simple_DifferentCase_IsDuplicate()
    {
        var table = Table(
            new[] { Col(0, "ФИО") },
            new[] { "Иванов" },
            new[] { "иванов" });

        var field = new Field("ФИО", FieldKind.Simple, new[] { 0 });

        var result = new DuplicateValidator().Validate(
            table, new[] { field });

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Simple_WhitespaceAround_IsDuplicate()
    {
        var table = Table(
            new[] { Col(0, "ФИО") },
            new[] { "Иванов" },
            new[] { "  Иванов  " });

        var field = new Field("ФИО", FieldKind.Simple, new[] { 0 });

        var result = new DuplicateValidator().Validate(
            table, new[] { field });

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Simple_YoAndYe_AreSame()
    {
        var table = Table(
            new[] { Col(0, "ФИО") },
            new[] { "Фёдоров" },
            new[] { "Федоров" });

        var field = new Field("ФИО", FieldKind.Simple, new[] { 0 });

        var result = new DuplicateValidator().Validate(
            table, new[] { field });

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Simple_EmptyValues_NotDuplicates()
    {
        var table = Table(
            new[] { Col(0, "ФИО") },
            new[] { "" },
            new[] { "   " },
            new[] { "" });

        var field = new Field("ФИО", FieldKind.Simple, new[] { 0 });

        var result = new DuplicateValidator().Validate(
            table, new[] { field });

        Assert.Empty(result);
    }

    // ---------------------------------------------------------------------
    // FullName
    // ---------------------------------------------------------------------

    [Fact]
    public void FullName_DuplicateAfterJoin()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя") },
            new[] { "Иванов", "Иван" },
            new[] { "Петров", "Пётр" },
            new[] { "Иванов", "Иван" });

        var field = new Field("ФИО", FieldKind.FullName, new[] { 0, 1 });

        var result = new DuplicateValidator().Validate(
            table, new[] { field });

        Assert.Equal(2, result.Count);
        Assert.Equal(0, result[0].RowIndex);
        Assert.Equal(2, result[1].RowIndex);
    }

    // ---------------------------------------------------------------------
    // Static — пропускается
    // ---------------------------------------------------------------------

    [Fact]
    public void Static_IsSkipped()
    {
        var table = Table(
            new[] { Col(0, "ФИО") },
            new[] { "Иванов" },
            new[] { "Петров" });

        var field = new Field(
            "Дата", FieldKind.Static, Array.Empty<int>(),
            staticValue: "1 сентября 2026");

        var result = new DuplicateValidator().Validate(
            table, new[] { field });

        Assert.Empty(result);
    }

    // ---------------------------------------------------------------------
    // Несколько полей
    // ---------------------------------------------------------------------

    [Fact]
    public void MultipleFields_DuplicatesInEach()
    {
        var table = Table(
            new[] { Col(0, "ФИО"), Col(1, "Класс") },
            new[] { "Иванов", "5А" },
            new[] { "Иванов", "6Б" },
            new[] { "Петров", "5А" });

        var fio = new Field("ФИО", FieldKind.Simple, new[] { 0 });
        var klass = new Field("Класс", FieldKind.Simple, new[] { 1 });

        var result = new DuplicateValidator().Validate(
            table, new[] { fio, klass });

        // ФИО: 2 (строки 0 и 1)
        // Класс: 2 (строки 0 и 2)
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void MultipleFields_ResultsAreOrderedByFieldThenRow()
    {
        var table = Table(
            new[] { Col(0, "ФИО"), Col(1, "Класс") },
            new[] { "Иванов", "5А" },
            new[] { "Иванов", "6Б" },
            new[] { "Петров", "5А" });

        var fio = new Field("ФИО", FieldKind.Simple, new[] { 0 });
        var klass = new Field("Класс", FieldKind.Simple, new[] { 1 });

        var result = new DuplicateValidator().Validate(
            table, new[] { fio, klass });

        // Сначала все проблемы поля «ФИО», потом все проблемы поля «Класс».
        Assert.Equal("ФИО", result[0].FieldName);
        Assert.Equal("ФИО", result[1].FieldName);
        Assert.Equal("Класс", result[2].FieldName);
        Assert.Equal("Класс", result[3].FieldName);
    }
}