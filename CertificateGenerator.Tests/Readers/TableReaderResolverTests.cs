using CertificateGenerator.Infrastructure.Readers;
using Xunit;

namespace CertificateGenerator.Tests.Readers;

public class TableReaderResolverTests
{
    private readonly TableReaderResolver _resolver = new();

    [Fact]
    public void Resolve_Xlsx_Returns_ClosedXmlReader()
    {
        var reader = _resolver.Resolve("file.xlsx");

        Assert.IsType<ClosedXmlTableReader>(reader);
    }

    [Fact]
    public void Resolve_Csv_Returns_CsvReader()
    {
        var reader = _resolver.Resolve("file.csv");

        Assert.IsType<CsvTableReader>(reader);
    }

    [Fact]
    public void Resolve_Xlsx_Uppercase_Returns_ClosedXmlReader()
    {
        var reader = _resolver.Resolve("file.XLSX");

        Assert.IsType<ClosedXmlTableReader>(reader);
    }

    [Fact]
    public void Resolve_Csv_MixedCase_Returns_CsvReader()
    {
        var reader = _resolver.Resolve("file.Csv");

        Assert.IsType<CsvTableReader>(reader);
    }

    [Fact]
    public void Resolve_Full_Path_Works()
    {
        var reader = _resolver.Resolve(@"C:\some\path\file.xlsx");

        Assert.IsType<ClosedXmlTableReader>(reader);
    }

    [Fact]
    public void Resolve_Throws_On_Unsupported_Extension()
    {
        Assert.Throws<NotSupportedException>(
            () => _resolver.Resolve("file.txt"));
    }

    [Fact]
    public void Resolve_Throws_On_No_Extension()
    {
        Assert.Throws<NotSupportedException>(
            () => _resolver.Resolve("file"));
    }

    [Fact]
    public void Resolve_Throws_On_Null_Path()
    {
        Assert.Throws<ArgumentNullException>(
            () => _resolver.Resolve(null!));
    }

    [Fact]
    public void Resolve_Throws_On_Empty_Path()
    {
        Assert.Throws<ArgumentException>(
            () => _resolver.Resolve(""));
    }

    [Fact]
    public void Resolve_Throws_On_Whitespace_Path()
    {
        Assert.Throws<ArgumentException>(
            () => _resolver.Resolve("   "));
    }

    [Fact]
    public void Resolve_Returns_Same_Instance_For_Same_Extension()
    {
        var first = _resolver.Resolve("a.xlsx");
        var second = _resolver.Resolve("b.xlsx");

        Assert.Same(first, second);
    }
}