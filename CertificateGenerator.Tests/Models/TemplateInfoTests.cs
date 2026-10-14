using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Tests.Models;

public sealed class TemplateInfoTests
{
    [Fact]
    public void Constructor_AllValuesStoredCorrectly()
    {
        var info = new TemplateInfo("template.png", 2480, 3508);

        Assert.Equal("template.png", info.FileName);
        Assert.Equal(2480, info.Width);
        Assert.Equal(3508, info.Height);
    }

    [Fact]
    public void Constructor_EmptyFileName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new TemplateInfo(string.Empty, 100, 100));
    }

    [Fact]
    public void Constructor_WhitespaceFileName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new TemplateInfo("   ", 100, 100));
    }

    [Fact]
    public void Constructor_NullFileName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new TemplateInfo(null!, 100, 100));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_NonPositiveWidth_Throws(int width)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TemplateInfo("t.png", width, 100));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_NonPositiveHeight_Throws(int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TemplateInfo("t.png", 100, height));
    }
}