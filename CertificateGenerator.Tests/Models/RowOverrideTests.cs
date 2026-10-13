using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Tests.Models;

public class RowOverrideTests
{
    [Fact]
    public void Constructor_StoresRowIndexAndValues()
    {
        var values = new[] { "Иванов", "Иван", "Иванович" };

        var row = new RowOverride(5, values);

        Assert.Equal(5, row.RowIndex);
        Assert.Equal(3, row.Values.Count);
        Assert.Equal("Иванов", row.Values[0]);
    }

    [Fact]
    public void Constructor_NegativeRowIndex_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new RowOverride(-1, new[] { "Иванов" }));
    }

    [Fact]
    public void Constructor_NullValues_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RowOverride(0, null!));
    }

    [Fact]
    public void Constructor_EmptyValues_Allowed()
    {
        var row = new RowOverride(0, Array.Empty<string>());

        Assert.Empty(row.Values);
    }

    [Fact]
    public void Constructor_CopiesValues()
    {
        // Если снаружи изменят исходный список — RowOverride не должен это увидеть.
        var values = new List<string> { "Иванов", "Иван" };

        var row = new RowOverride(0, values);
        values[0] = "Петров";

        Assert.Equal("Иванов", row.Values[0]);
    }

    [Fact]
    public void Values_IsNotSameReferenceAsSource()
    {
        var values = new[] { "Иванов" };

        var row = new RowOverride(0, values);

        Assert.NotSame(values, row.Values);
    }
}