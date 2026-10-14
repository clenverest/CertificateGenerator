using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Tests.Models;

public sealed class FieldPlacementTests
{
    private static FieldPlacement CreateSample() =>
        new(
            fieldName: "ФИО",
            x: 100,
            y: 200,
            fontSize: 48,
            fontFamily: "Arial",
            alignment: FieldAlignment.Center);

    [Fact]
    public void Constructor_AllValuesStoredCorrectly()
    {
        var placement = CreateSample();

        Assert.Equal("ФИО", placement.FieldName);
        Assert.Equal(100, placement.X);
        Assert.Equal(200, placement.Y);
        Assert.Equal(48, placement.FontSize);
        Assert.Equal("Arial", placement.FontFamily);
        Assert.Equal(FieldAlignment.Center, placement.Alignment);
        Assert.Equal("#000000", placement.TextColor);
    }

    [Fact]
    public void Constructor_EmptyFieldName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new FieldPlacement(string.Empty, 0, 0, 12, "Arial", FieldAlignment.Left));
    }

    [Fact]
    public void Constructor_WhitespaceFieldName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new FieldPlacement("   ", 0, 0, 12, "Arial", FieldAlignment.Left));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveFontSize_Throws(int fontSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FieldPlacement("ФИО", 0, 0, fontSize, "Arial", FieldAlignment.Left));
    }

    [Fact]
    public void Constructor_EmptyFontFamily_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new FieldPlacement("ФИО", 0, 0, 12, string.Empty, FieldAlignment.Left));
    }

    [Fact]
    public void Constructor_EmptyTextColor_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new FieldPlacement("ФИО", 0, 0, 12, "Arial", FieldAlignment.Left, ""));
    }

    [Fact]
    public void Constructor_CustomTextColor_Stored()
    {
        var placement = new FieldPlacement(
            "ФИО", 0, 0, 12, "Arial", FieldAlignment.Left, "#FF0000");

        Assert.Equal("#FF0000", placement.TextColor);
    }

    // ---------------------------------------------------------------------
    // Иммутабельные обновления
    // ---------------------------------------------------------------------

    [Fact]
    public void WithX_ReturnsNewInstance_OriginalUnchanged()
    {
        var original = CreateSample();
        var updated = original.WithX(500);

        Assert.Equal(100, original.X);
        Assert.Equal(500, updated.X);
        Assert.NotSame(original, updated);
    }

    [Fact]
    public void WithY_ReturnsNewInstance_OriginalUnchanged()
    {
        var original = CreateSample();
        var updated = original.WithY(999);

        Assert.Equal(200, original.Y);
        Assert.Equal(999, updated.Y);
    }

    [Fact]
    public void WithPosition_ChangesBothCoordinates()
    {
        var original = CreateSample();
        var updated = original.WithPosition(10, 20);

        Assert.Equal(100, original.X);
        Assert.Equal(200, original.Y);
        Assert.Equal(10, updated.X);
        Assert.Equal(20, updated.Y);
    }

    [Fact]
    public void WithFontSize_ReturnsNewInstance_OriginalUnchanged()
    {
        var original = CreateSample();
        var updated = original.WithFontSize(72);

        Assert.Equal(48, original.FontSize);
        Assert.Equal(72, updated.FontSize);
    }

    [Fact]
    public void WithFontFamily_ReturnsNewInstance_OriginalUnchanged()
    {
        var original = CreateSample();
        var updated = original.WithFontFamily("Times New Roman");

        Assert.Equal("Arial", original.FontFamily);
        Assert.Equal("Times New Roman", updated.FontFamily);
    }

    [Fact]
    public void WithAlignment_ReturnsNewInstance_OriginalUnchanged()
    {
        var original = CreateSample();
        var updated = original.WithAlignment(FieldAlignment.Right);

        Assert.Equal(FieldAlignment.Center, original.Alignment);
        Assert.Equal(FieldAlignment.Right, updated.Alignment);
    }

    [Fact]
    public void WithTextColor_ReturnsNewInstance_OriginalUnchanged()
    {
        var original = CreateSample();
        var updated = original.WithTextColor("#FF0000");

        Assert.Equal("#000000", original.TextColor);
        Assert.Equal("#FF0000", updated.TextColor);
    }
}