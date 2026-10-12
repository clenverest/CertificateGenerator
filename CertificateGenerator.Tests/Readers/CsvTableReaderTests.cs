using CertificateGenerator.Infrastructure.Readers;
using Xunit;

namespace CertificateGenerator.Tests.Readers;

public class CsvTableReaderTests : IDisposable
{
    private readonly TestCsvFactory _factory = new();

    [Fact]
    public void Read_Comma_Separated_Utf8_Returns_Table()
    {
        string content = "ФИО,Класс\nИванов Иван,5А\nПетров Пётр,5Б";
        string path = _factory.CreateUtf8File("comma.csv", content);

        var reader = new CsvTableReader();
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
    public void Read_Semicolon_Separated_Utf8_Returns_Table()
    {
        string content = "ФИО;Класс\nИванов Иван;5А\nПетров Пётр;5Б";
        string path = _factory.CreateUtf8File("semicolon.csv", content);

        var reader = new CsvTableReader();
        var table = reader.Read(path);

        Assert.Equal(2, table.ColumnCount);
        Assert.Equal(2, table.RowCount);
        Assert.Equal("ФИО", table.Columns[0].Name);
        Assert.Equal("Иванов Иван", table.Rows[0][0]);
        Assert.Equal("5А", table.Rows[0][1]);
    }

    [Fact]
    public void Read_Utf8_Bom_File()
    {
        string content = "ФИО\nИванов Иван";
        string path = _factory.CreateUtf8BomFile("utf8bom.csv", content);

        var reader = new CsvTableReader();
        var table = reader.Read(path);

        Assert.Equal(1, table.ColumnCount);
        Assert.Equal(1, table.RowCount);
        Assert.Equal("ФИО", table.Columns[0].Name);
        Assert.Equal("Иванов Иван", table.Rows[0][0]);
    }

    [Fact]
    public void Read_Windows1251_File()
    {
        string content = "ФИО\nИванов Иван";
        string path = _factory.CreateWindows1251File("cp1251.csv", content);

        var reader = new CsvTableReader();
        var table = reader.Read(path);

        Assert.Equal(1, table.ColumnCount);
        Assert.Equal(1, table.RowCount);
        Assert.Equal("ФИО", table.Columns[0].Name);
        Assert.Equal("Иванов Иван", table.Rows[0][0]);
    }

    [Fact]
    public void Read_File_With_Only_Headers_Returns_Empty_Rows()
    {
        string content = "ФИО\n";
        string path = _factory.CreateUtf8File("headers-only.csv", content);

        var reader = new CsvTableReader();
        var table = reader.Read(path);

        Assert.Equal(1, table.ColumnCount);
        Assert.Equal(0, table.RowCount);
        Assert.Equal("ФИО", table.Columns[0].Name);
    }

    [Fact]
    public void Read_Trims_Empty_Rows_At_End()
    {
        string content = "ФИО\nA\nB\n\n  \n";
        string path = _factory.CreateUtf8File("trailing-empty.csv", content);

        var reader = new CsvTableReader();
        var table = reader.Read(path);

        Assert.Equal(2, table.RowCount);
    }

    [Fact]
    public void Read_Keeps_Empty_Rows_In_Middle()
    {
        string content = "ФИО\nA\n\nB";
        string path = _factory.CreateUtf8File("middle-empty.csv", content);

        var reader = new CsvTableReader();
        var table = reader.Read(path);

        Assert.Equal(3, table.RowCount);
        Assert.Equal("A", table.Rows[0][0]);
        Assert.Equal("", table.Rows[1][0]);
        Assert.Equal("B", table.Rows[2][0]);
    }

    [Fact]
    public void Read_Quoted_Field_With_Delimiter()
    {
        string content = "ФИО;Класс\n\"Иванов, Иван\";5А";
        string path = _factory.CreateUtf8File("quoted.csv", content);

        var reader = new CsvTableReader();
        var table = reader.Read(path);

        Assert.Equal(2, table.ColumnCount);
        Assert.Equal(1, table.RowCount);
        Assert.Equal("Иванов, Иван", table.Rows[0][0]);
        Assert.Equal("5А", table.Rows[0][1]);
    }

    [Fact]
    public void Read_Throws_On_Null_Path()
    {
        var reader = new CsvTableReader();

        Assert.Throws<ArgumentNullException>(
            () => reader.Read(null!));
    }

    [Fact]
    public void Read_Throws_On_Empty_Path()
    {
        var reader = new CsvTableReader();

        Assert.Throws<ArgumentException>(
            () => reader.Read(""));
    }

    [Fact]
    public void Read_Throws_On_NonExistent_File()
    {
        var reader = new CsvTableReader();

        Assert.Throws<FileNotFoundException>(
            () => reader.Read("nonexistent-file.csv"));
    }

    [Fact]
    public void Read_Throws_On_Empty_File()
    {
        string path = _factory.CreateUtf8File("empty.csv", "");

        var reader = new CsvTableReader();

        Assert.Throws<InvalidDataException>(
            () => reader.Read(path));
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}