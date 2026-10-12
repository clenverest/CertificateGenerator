using CertificateGenerator.Core.Models;
using Xunit;

namespace CertificateGenerator.Tests.Fakes;

public class FakeTableReaderTests
{
    private static TableData CreateSampleTable()
    {
        var columns = new List<ColumnInfo>
        {
            new ColumnInfo(0, "ФИО"),
        };
        var rows = new List<IReadOnlyList<string>>
        {
            new List<string> { "Иванов Иван" },
        };
        return new TableData(columns, rows);
    }

    [Fact]
    public void Read_Returns_Configured_Table()
    {
        var table = CreateSampleTable();
        var reader = new FakeTableReader(table);

        var result = reader.Read("any/path.xlsx");

        Assert.Same(table, result);
    }

    [Fact]
    public void Read_Throws_On_Null_Path()
    {
        var table = CreateSampleTable();
        var reader = new FakeTableReader(table);

        Assert.Throws<ArgumentNullException>(
            () => reader.Read(null!));
    }

    [Fact]
    public void Constructor_Throws_On_Null_Table()
    {
        Assert.Throws<ArgumentNullException>(
            () => new FakeTableReader(null!));
    }

    [Fact]
    public void Resolve_Returns_Reader_For_Xlsx()
    {
        var table = CreateSampleTable();
        var reader = new FakeTableReader(table);
        var resolver = new FakeTableReaderResolver(reader);

        var result = resolver.Resolve("file.xlsx");

        Assert.Same(reader, result);
    }

    [Fact]
    public void Resolve_Returns_Reader_For_Csv()
    {
        var table = CreateSampleTable();
        var reader = new FakeTableReader(table);
        var resolver = new FakeTableReaderResolver(reader);

        var result = resolver.Resolve("file.csv");

        Assert.Same(reader, result);
    }

    [Fact]
    public void Resolve_Is_Case_Insensitive()
    {
        var table = CreateSampleTable();
        var reader = new FakeTableReader(table);
        var resolver = new FakeTableReaderResolver(reader);

        var result = resolver.Resolve("file.XLSX");

        Assert.Same(reader, result);
    }

    [Fact]
    public void Resolve_Throws_On_Unsupported_Extension()
    {
        var table = CreateSampleTable();
        var reader = new FakeTableReader(table);
        var resolver = new FakeTableReaderResolver(reader);

        Assert.Throws<NotSupportedException>(
            () => resolver.Resolve("file.txt"));
    }
}