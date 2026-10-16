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
    // Хелперы для полей
    // ---------------------------------------------------------------------

    private static Field RequiredFullName(int[]? indexes = null)
    {
        return new Field(
            "ФИО", FieldKind.FullName,
            indexes ?? new[] { 0, 1 },
            isRequired: true);
    }

    private static Field OptionalFullName(int[]? indexes = null)
    {
        return new Field(
            "ФИО", FieldKind.FullName,
            indexes ?? new[] { 0, 1 },
            isRequired: false);
    }

    private static Field RequiredSimple(string name = "Класс")
    {
        return new Field(name, FieldKind.Simple, new[] { 0 }, isRequired: true);
    }

    private static Field OptionalSimple(string name = "Класс")
    {
        return new Field(name, FieldKind.Simple, new[] { 0 }, isRequired: false);
    }

    private static Field StaticField()
    {
        return new Field(
            "Дата", FieldKind.Static, Array.Empty<int>(),
            staticValue: "1 сентября 2026");
    }

    // ---------------------------------------------------------------------
    // Нет работы — нет проблем
    // ---------------------------------------------------------------------

    [Fact]
    public void EmptyFields_ReturnsEmpty()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя") },
            new[] { "Иванов", "Иван" },
            new[] { "Иванов", "Иван" });

        var result = new DuplicateValidator().Validate(
            table, Array.Empty<Field>());

        Assert.Empty(result);
    }

    [Fact]
    public void OneRow_ReturnsEmpty()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя") },
            new[] { "Иванов", "Иван" });

        var result = new DuplicateValidator().Validate(
            table, new[] { RequiredFullName() });

        Assert.Empty(result);
    }

    [Fact]
    public void AllUnique_ReturnsEmpty()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя") },
            new[] { "Иванов", "Иван" },
            new[] { "Петров", "Пётр" },
            new[] { "Сидоров", "Сидор" });

        var result = new DuplicateValidator().Validate(
            table, new[] { RequiredFullName() });

        Assert.Empty(result);
    }

    // ---------------------------------------------------------------------
    // Обязательное FullName — проверяется
    // ---------------------------------------------------------------------

    [Fact]
    public void RequiredFullName_TwoIdentical_ReturnsTwoWarnings()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя") },
            new[] { "Иванов", "Иван" },
            new[] { "Петров", "Пётр" },
            new[] { "Иванов", "Иван" });

        var result = new DuplicateValidator().Validate(
            table, new[] { RequiredFullName() });

        Assert.Equal(2, result.Count);
        Assert.All(result, i => Assert.Equal(IssueSeverity.Warning, i.Severity));
        Assert.All(result, i => Assert.Equal("ФИО", i.FieldName));
        Assert.Equal(0, result[0].RowIndex);
        Assert.Equal(2, result[1].RowIndex);
    }

    [Fact]
    public void RequiredFullName_ThreeIdentical_ReturnsThreeWarnings()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя") },
            new[] { "Иванов", "Иван" },
            new[] { "Иванов", "Иван" },
            new[] { "Иванов", "Иван" });

        var result = new DuplicateValidator().Validate(
            table, new[] { RequiredFullName() });

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void RequiredFullName_DifferentCase_IsDuplicate()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя") },
            new[] { "Иванов", "Иван" },
            new[] { "иванов", "иван" });

        var result = new DuplicateValidator().Validate(
            table, new[] { RequiredFullName() });

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void RequiredFullName_WhitespaceAround_IsDuplicate()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя") },
            new[] { "Иванов", "Иван" },
            new[] { "  Иванов  ", "  Иван  " });

        var result = new DuplicateValidator().Validate(
            table, new[] { RequiredFullName() });

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void RequiredFullName_YoAndYe_AreSame()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя") },
            new[] { "Фёдоров", "Пётр" },
            new[] { "Федоров", "Петр" });

        var result = new DuplicateValidator().Validate(
            table, new[] { RequiredFullName() });

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void RequiredFullName_EmptyValues_NotDuplicates()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя") },
            new[] { "", "" },
            new[] { "   ", "" },
            new[] { "", "   " });

        var result = new DuplicateValidator().Validate(
            table, new[] { RequiredFullName() });

        Assert.Empty(result);
    }

    [Fact]
    public void RequiredFullName_MessageContainsFieldNameAndValue()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя") },
            new[] { "Иванов", "Иван" },
            new[] { "Иванов", "Иван" });

        var result = new DuplicateValidator().Validate(
            table, new[] { RequiredFullName() });

        Assert.Contains("ФИО", result[0].Message);
        Assert.Contains("Иванов Иван", result[0].Message);
    }

    // ---------------------------------------------------------------------
    // Необязательное FullName — не проверяется
    // ---------------------------------------------------------------------

    [Fact]
    public void OptionalFullName_NotChecked_EvenIfDuplicates()
    {
        var table = Table(
            new[] { Col(0, "Фамилия"), Col(1, "Имя") },
            new[] { "Иванов", "Иван" },
            new[] { "Иванов", "Иван" });

        var result = new DuplicateValidator().Validate(
            table, new[] { OptionalFullName() });

        Assert.Empty(result);
    }

    // ---------------------------------------------------------------------
    // Обязательное Simple — проверяется
    // ---------------------------------------------------------------------

    [Fact]
    public void RequiredSimple_TwoIdentical_ReturnsTwoWarnings()
    {
        var table = Table(
            new[] { Col(0, "Класс") },
            new[] { "5А" },
            new[] { "6Б" },
            new[] { "5А" });

        var result = new DuplicateValidator().Validate(
            table, new[] { RequiredSimple() });

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void RequiredSimple_NotDuplicated_ReturnsEmpty()
    {
        var table = Table(
            new[] { Col(0, "Класс") },
            new[] { "5А" },
            new[] { "6Б" });

        var result = new DuplicateValidator().Validate(
            table, new[] { RequiredSimple() });

        Assert.Empty(result);
    }

    // ---------------------------------------------------------------------
    // Необязательное Simple — не проверяется
    // ---------------------------------------------------------------------

    [Fact]
    public void OptionalSimple_NotChecked_EvenIfDuplicates()
    {
        var table = Table(
            new[] { Col(0, "Класс") },
            new[] { "5А" },
            new[] { "5А" },
            new[] { "5А" });

        var result = new DuplicateValidator().Validate(
            table, new[] { OptionalSimple() });

        Assert.Empty(result);
    }

    // ---------------------------------------------------------------------
    // Static — не проверяется всегда
    // ---------------------------------------------------------------------

    [Fact]
    public void Static_NotChecked_EvenIfRequired()
    {
        var table = Table(
            new[] { Col(0, "ФИО") },
            new[] { "Иванов" },
            new[] { "Петров" });

        var field = new Field(
            "Дата", FieldKind.Static, Array.Empty<int>(),
            isRequired: true,
            staticValue: "1 сентября 2026");

        var result = new DuplicateValidator().Validate(
            table, new[] { field });

        Assert.Empty(result);
    }

    // ---------------------------------------------------------------------
    // Несколько полей
    // ---------------------------------------------------------------------

    [Fact]
    public void MultipleFields_OnlyRequiredChecked()
    {
        var table = Table(
            new[]
            {
                Col(0, "Фамилия"),
                Col(1, "Имя"),
                Col(2, "Класс")
            },
            new[] { "Иванов", "Иван", "5А" },
            new[] { "Иванов", "Иван", "5А" });

        var fio = new Field("ФИО", FieldKind.FullName, new[] { 0, 1 }, isRequired: true);
        var klass = new Field("Класс", FieldKind.Simple, new[] { 2 }, isRequired: false);

        var result = new DuplicateValidator().Validate(
            table, new[] { fio, klass });

        // Только ФИО даёт предупреждения — 2 штуки.
        Assert.Equal(2, result.Count);
        Assert.All(result, i => Assert.Equal("ФИО", i.FieldName));
    }

    [Fact]
    public void MultipleFields_BothRequired_ReportsEach()
    {
        var table = Table(
            new[]
            {
                Col(0, "Фамилия"),
                Col(1, "Имя"),
                Col(2, "Класс")
            },
            new[] { "Иванов", "Иван", "5А" },
            new[] { "Иванов", "Иван", "5А" });

        var fio = new Field("ФИО", FieldKind.FullName, new[] { 0, 1 }, isRequired: true);
        var klass = new Field("Класс", FieldKind.Simple, new[] { 2 }, isRequired: true);

        var result = new DuplicateValidator().Validate(
            table, new[] { fio, klass });

        // ФИО × 2 + Класс × 2 = 4 предупреждения.
        Assert.Equal(4, result.Count);
    }
}