using CertificateGenerator.Core.Models;
using Xunit;

namespace CertificateGenerator.Tests.Models;

public class TableDataTests
{
    // ---------- Успешные сценарии ----------

    [Fact]
    public void Valid_Table_With_Data()
    {
        var columns = new List<ColumnInfo>
        {
            new ColumnInfo(0, "ФИО"),
            new ColumnInfo(1, "Класс"),
        };
        var rows = new List<IReadOnlyList<string>>
        {
            new List<string> { "Иванов Иван", "5А" },
            new List<string> { "Петров Пётр", "5Б" },
            new List<string> { "Сидоров Сидор", "5В" },
        };

        var table = new TableData(columns, rows);

        Assert.Equal(3, table.RowCount);
        Assert.Equal(2, table.ColumnCount);
        Assert.Equal("Иванов Иван", table.Rows[0][0]);
        Assert.Equal("5А", table.Rows[0][1]);
        Assert.Equal("Сидоров Сидор", table.Rows[2][0]);
    }

    [Fact]
    public void Empty_Rows_Is_Allowed()
    {
        var columns = new List<ColumnInfo>
        {
            new ColumnInfo(0, "ФИО"),
            new ColumnInfo(1, "Класс"),
        };
        var rows = new List<IReadOnlyList<string>>();

        var table = new TableData(columns, rows);

        Assert.Equal(0, table.RowCount);
        Assert.Equal(2, table.ColumnCount);
    }

    [Fact]
    public void Single_Column_Is_Allowed()
    {
        var columns = new List<ColumnInfo>
        {
            new ColumnInfo(0, "ФИО"),
        };
        var rows = new List<IReadOnlyList<string>>
        {
            new List<string> { "Иванов Иван" },
        };

        var table = new TableData(columns, rows);

        Assert.Equal(1, table.RowCount);
        Assert.Equal(1, table.ColumnCount);
        Assert.Equal("Иванов Иван", table.Rows[0][0]);
    }

    // ---------- Ошибки ----------

    [Fact]
    public void Null_Columns_Throws()
    {
        var rows = new List<IReadOnlyList<string>>();

        Assert.Throws<ArgumentNullException>(
            () => new TableData(null!, rows));
    }

    [Fact]
    public void Empty_Columns_Throws()
    {
        var columns = new List<ColumnInfo>();
        var rows = new List<IReadOnlyList<string>>();

        Assert.Throws<ArgumentException>(
            () => new TableData(columns, rows));
    }

    [Fact]
    public void Null_Rows_Throws()
    {
        var columns = new List<ColumnInfo>
        {
            new ColumnInfo(0, "ФИО"),
        };

        Assert.Throws<ArgumentNullException>(
            () => new TableData(columns, null!));
    }

    [Fact]
    public void Row_Shorter_Than_Columns_Throws()
    {
        var columns = new List<ColumnInfo>
        {
            new ColumnInfo(0, "ФИО"),
            new ColumnInfo(1, "Класс"),
        };
        var rows = new List<IReadOnlyList<string>>
        {
            new List<string> { "Иванов Иван" },
        };

        Assert.Throws<ArgumentException>(
            () => new TableData(columns, rows));
    }

    [Fact]
    public void Row_Longer_Than_Columns_Throws()
    {
        var columns = new List<ColumnInfo>
        {
            new ColumnInfo(0, "ФИО"),
            new ColumnInfo(1, "Класс"),
        };
        var rows = new List<IReadOnlyList<string>>
        {
            new List<string> { "Иванов Иван", "5А", "Лишнее" },
        };

        Assert.Throws<ArgumentException>(
            () => new TableData(columns, rows));
    }
}