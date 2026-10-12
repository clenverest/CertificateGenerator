using CertificateGenerator.Core.Models;
using Xunit;

namespace CertificateGenerator.Tests.Models;

public class ColumnInfoTests
{
    [Fact]
    public void Constructor_Sets_Index_And_Name()
    {
        var column = new ColumnInfo(0, "ФИО");

        Assert.Equal(0, column.Index);
        Assert.Equal("ФИО", column.Name);
    }

    [Fact]
    public void Index_Zero_Is_Allowed()
    {
        var column = new ColumnInfo(0, "Первая");

        Assert.Equal(0, column.Index);
    }

    [Fact]
    public void Large_Index_Is_Allowed()
    {
        var column = new ColumnInfo(100, "X");

        Assert.Equal(100, column.Index);
    }

    [Fact]
    public void Empty_Name_Is_Allowed()
    {
        var column = new ColumnInfo(0, "");

        Assert.Equal("", column.Name);
    }
}