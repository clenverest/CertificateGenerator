using CertificateGenerator.Infrastructure.Readers;
using Xunit;

namespace CertificateGenerator.Tests.Readers;

public class ClosedXmlTableReaderTests : IDisposable
{
    private readonly TestXlsxFactory _factory = new();

    [Fact]
    public void Read_Valid_File_Returns_Table()
    {
        var headers = new[] { "ФИО", "Класс" };
        var rows = new List<IReadOnlyList<string>>
        {
            new List<string> { "Иванов Иван", "5А" },
            new List<string> { "Петров Пётр", "5Б" },
        };
        string path = _factory.CreateSimpleFile("valid.xlsx", headers, rows);

        var reader = new ClosedXmlTableReader();
        var table = reader.Read(path);

        Assert.Equal(2, table.ColumnCount);
        Assert.Equal(2, table.RowCount);
        Assert.Equal("ФИО", table.Columns[0].Name);
        Assert.Equal("Класс", table.Columns[1].Name);
        Assert.Equal("Иванов Иван", table.Rows[0][0]);
        Assert.Equal("5А", table.Rows[0][1]);
        Assert.Equal("Петров Пётр", table.Rows[1][0]);
    }

    [Fact]
    public void Read_File_With_Only_Headers_Returns_Empty_Rows()
    {
        var headers = new[] { "ФИО" };
        var rows = new List<IReadOnlyList<string>>();
        string path = _factory.CreateSimpleFile("headers-only.xlsx", headers, rows);

        var reader = new ClosedXmlTableReader();
        var table = reader.Read(path);

        Assert.Equal(1, table.ColumnCount);
        Assert.Equal(0, table.RowCount);
        Assert.Equal("ФИО", table.Columns[0].Name);
    }

    [Fact]
    public void Read_Trims_Empty_Rows_At_End()
    {
        string path = _factory.CreateFile("trailing-empty.xlsx", ws =>
        {
            ws.Cell(1, 1).Value = "ФИО";
            ws.Cell(2, 1).Value = "Иванов Иван";
            ws.Cell(3, 1).Value = "Петров Пётр";
            ws.Cell(4, 1).Value = "";
            ws.Cell(5, 1).Value = "  ";
            ws.Cell(6, 1).Value = "";
        });

        var reader = new ClosedXmlTableReader();
        var table = reader.Read(path);

        Assert.Equal(1, table.ColumnCount);
        Assert.Equal(2, table.RowCount);
    }

    [Fact]
    public void Read_Keeps_Empty_Rows_In_Middle()
    {
        string path = _factory.CreateFile("middle-empty.xlsx", ws =>
        {
            ws.Cell(1, 1).Value = "ФИО";
            ws.Cell(2, 1).Value = "A";
            ws.Cell(3, 1).Value = "";
            ws.Cell(4, 1).Value = "B";
        });

        var reader = new ClosedXmlTableReader();
        var table = reader.Read(path);

        Assert.Equal(3, table.RowCount);
        Assert.Equal("A", table.Rows[0][0]);
        Assert.Equal("", table.Rows[1][0]);
        Assert.Equal("B", table.Rows[2][0]);
    }

    [Fact]
    public void Read_Throws_On_Null_Path()
    {
        var reader = new ClosedXmlTableReader();

        Assert.Throws<ArgumentNullException>(
            () => reader.Read(null!));
    }

    [Fact]
    public void Read_Throws_On_Empty_Path()
    {
        var reader = new ClosedXmlTableReader();

        Assert.Throws<ArgumentException>(
            () => reader.Read(""));
    }

    [Fact]
    public void Read_Throws_On_NonExistent_File()
    {
        var reader = new ClosedXmlTableReader();

        Assert.Throws<FileNotFoundException>(
            () => reader.Read("nonexistent-file.xlsx"));
    }

    [Fact]
    public void Read_Throws_On_Empty_Sheet()
    {
        string path = _factory.CreateFile("empty-sheet.xlsx", _ => { });

        var reader = new ClosedXmlTableReader();

        Assert.Throws<InvalidDataException>(
            () => reader.Read(path));
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}