using CertificateGenerator.Core.Models;
using CertificateGenerator.Core.Services;

namespace CertificateGenerator.Tests.Services;

public class FieldValueResolverTests
{
    // ---------------------------------------------------------------------
    // Simple
    // ---------------------------------------------------------------------

    [Fact]
    public void Simple_ReturnsCellValue()
    {
        var field = new Field("Класс", FieldKind.Simple, new[] { 1 });
        var row = new[] { "Иванов", "5А", "Москва" };

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal("5А", result);
    }

    [Fact]
    public void Simple_TrimsWhitespace()
    {
        var field = new Field("Класс", FieldKind.Simple, new[] { 0 });
        var row = new[] { "  5А  " };

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal("5А", result);
    }

    [Fact]
    public void Simple_EmptyCell_ReturnsEmpty()
    {
        var field = new Field("Класс", FieldKind.Simple, new[] { 0 });
        var row = new[] { "   " };

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Simple_IndexOutOfRange_ReturnsEmpty()
    {
        var field = new Field("Класс", FieldKind.Simple, new[] { 5 });
        var row = new[] { "Иванов", "5А" };

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal(string.Empty, result);
    }

    // ---------------------------------------------------------------------
    // Static
    // ---------------------------------------------------------------------

    [Fact]
    public void Static_ReturnsStaticValue()
    {
        var field = new Field(
            "Дата", FieldKind.Static, Array.Empty<int>(),
            staticValue: "1 сентября 2026");
        var row = new[] { "Иванов", "5А" };

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal("1 сентября 2026", result);
    }

    [Fact]
    public void Static_IgnoresRow()
    {
        var field = new Field(
            "Школа", FieldKind.Static, Array.Empty<int>(),
            staticValue: "Гимназия №1");
        var row = Array.Empty<string>();

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal("Гимназия №1", result);
    }

    // ---------------------------------------------------------------------
    // FullName — одна колонка
    // ---------------------------------------------------------------------

    [Fact]
    public void FullName_OneColumn_ReturnsValue()
    {
        var field = new Field("ФИО", FieldKind.FullName, new[] { 0 });
        var row = new[] { "Иванов Иван Иванович" };

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal("Иванов Иван Иванович", result);
    }

    // ---------------------------------------------------------------------
    // FullName — три колонки
    // ---------------------------------------------------------------------

    [Fact]
    public void FullName_ThreeColumns_JoinsWithSpace()
    {
        var field = new Field("ФИО", FieldKind.FullName, new[] { 0, 1, 2 });
        var row = new[] { "Иванов", "Иван", "Иванович" };

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal("Иванов Иван Иванович", result);
    }

    [Fact]
    public void FullName_MiddleEmpty_SkipsIt()
    {
        var field = new Field("ФИО", FieldKind.FullName, new[] { 0, 1, 2 });
        var row = new[] { "Иванов", "", "Иванович" };

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal("Иванов Иванович", result);
    }

    [Fact]
    public void FullName_FirstEmpty_SkipsIt()
    {
        var field = new Field("ФИО", FieldKind.FullName, new[] { 0, 1, 2 });
        var row = new[] { "", "Иван", "Иванович" };

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal("Иван Иванович", result);
    }

    [Fact]
    public void FullName_LastEmpty_SkipsIt()
    {
        var field = new Field("ФИО", FieldKind.FullName, new[] { 0, 1, 2 });
        var row = new[] { "Иванов", "Иван", "" };

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal("Иванов Иван", result);
    }

    [Fact]
    public void FullName_AllEmpty_ReturnsEmpty()
    {
        var field = new Field("ФИО", FieldKind.FullName, new[] { 0, 1, 2 });
        var row = new[] { "", "   ", "" };

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FullName_TrimsEachPart()
    {
        var field = new Field("ФИО", FieldKind.FullName, new[] { 0, 1, 2 });
        var row = new[] { "  Иванов  ", "  Иван  ", "  Иванович  " };

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal("Иванов Иван Иванович", result);
    }

    [Fact]
    public void FullName_IndexOutOfRange_SkipsIt()
    {
        var field = new Field("ФИО", FieldKind.FullName, new[] { 0, 1, 5 });
        var row = new[] { "Иванов", "Иван" };

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal("Иванов Иван", result);
    }

    [Fact]
    public void FullName_AllIndexesOutOfRange_ReturnsEmpty()
    {
        var field = new Field("ФИО", FieldKind.FullName, new[] { 5, 6, 7 });
        var row = new[] { "Иванов", "Иван" };

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FullName_CustomSeparator_UsesIt()
    {
        var field = new Field(
            "ФИО", FieldKind.FullName, new[] { 0, 1 },
            separator: ", ");
        var row = new[] { "Иванов", "Иван" };

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal("Иванов, Иван", result);
    }

    [Fact]
    public void FullName_TwoColumns_JoinsThem()
    {
        var field = new Field("ФИО", FieldKind.FullName, new[] { 0, 1 });
        var row = new[] { "Иванов", "Иван" };

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal("Иванов Иван", result);
    }

    [Fact]
    public void FullName_TwoColumns_SecondEmpty_ReturnsFirst()
    {
        var field = new Field("ФИО", FieldKind.FullName, new[] { 0, 1 });
        var row = new[] { "Иванов", "" };

        var result = FieldValueResolver.Resolve(field, row);

        Assert.Equal("Иванов", result);
    }

    [Fact]
    public void FullName_PreservesOrderOfSourceIndexes()
    {
        var field = new Field("ФИО", FieldKind.FullName, new[] { 2, 0, 1 });
        var row = new[] { "Иванов", "Иван", "Иванович" };

        var result = FieldValueResolver.Resolve(field, row);

        // Порядок — как в SourceColumnIndexes: [2, 0, 1] → отчество, фамилия, имя.
        Assert.Equal("Иванович Иванов Иван", result);
    }
}