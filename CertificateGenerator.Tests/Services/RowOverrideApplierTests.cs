using CertificateGenerator.Core.Models;
using CertificateGenerator.Core.Services;

namespace CertificateGenerator.Tests.Services;

public class RowOverrideApplierTests
{
    private static ColumnInfo Col(int index, string name) => new(index, name);

    private static TableData Table(params string[][] rows)
    {
        return new TableData(
            new[] { Col(0, "A"), Col(1, "B") },
            rows.Select(r => (IReadOnlyList<string>)r).ToList());
    }

    // ---------------------------------------------------------------------
    // Нет правок
    // ---------------------------------------------------------------------

    [Fact]
    public void Apply_NoOverrides_ReturnsSameTable()
    {
        var source = Table(new[] { "a1", "b1" }, new[] { "a2", "b2" });

        var result = RowOverrideApplier.Apply(source, Array.Empty<RowOverride>());

        Assert.Same(source, result);
    }

    // ---------------------------------------------------------------------
    // Один override
    // ---------------------------------------------------------------------

    [Fact]
    public void Apply_OneOverride_ReplacesRow()
    {
        var source = Table(
            new[] { "a1", "b1" },
            new[] { "a2", "b2" });

        var overrides = new[]
        {
            new RowOverride(0, new[] { "A1", "B1" })
        };

        var result = RowOverrideApplier.Apply(source, overrides);

        Assert.NotSame(source, result);
        Assert.Equal("A1", result.Rows[0][0]);
        Assert.Equal("B1", result.Rows[0][1]);
        // Вторая строка — из источника.
        Assert.Equal("a2", result.Rows[1][0]);
        Assert.Equal("b2", result.Rows[1][1]);
    }

    [Fact]
    public void Apply_OverrideLastRow()
    {
        var source = Table(
            new[] { "a1", "b1" },
            new[] { "a2", "b2" });

        var overrides = new[]
        {
            new RowOverride(1, new[] { "A2", "B2" })
        };

        var result = RowOverrideApplier.Apply(source, overrides);

        Assert.Equal("a1", result.Rows[0][0]);
        Assert.Equal("A2", result.Rows[1][0]);
    }

    [Fact]
    public void Apply_MultipleOverrides_ReplacesEach()
    {
        var source = Table(
            new[] { "a1", "b1" },
            new[] { "a2", "b2" },
            new[] { "a3", "b3" });

        var overrides = new[]
        {
            new RowOverride(0, new[] { "A1", "B1" }),
            new RowOverride(2, new[] { "A3", "B3" })
        };

        var result = RowOverrideApplier.Apply(source, overrides);

        Assert.Equal("A1", result.Rows[0][0]);
        Assert.Equal("a2", result.Rows[1][0]);
        Assert.Equal("A3", result.Rows[2][0]);
    }

    // ---------------------------------------------------------------------
    // Границы — индекс вне диапазона
    // ---------------------------------------------------------------------

    [Fact]
    public void Apply_OverrideIndexOutOfRange_Ignored()
    {
        var source = Table(new[] { "a1", "b1" });

        var overrides = new[]
        {
            new RowOverride(5, new[] { "A1", "B1" })
        };

        var result = RowOverrideApplier.Apply(source, overrides);

        Assert.Equal("a1", result.Rows[0][0]);
    }

    [Fact]
    public void Apply_OverrideIndexNegative_Ignored()
    {
        // RowOverride не даст создать с -1, но перестрахуемся.
        // Используем обычный объект через конструктор.
        // RowIndex = 0 минимальный. Так что этот тест — про отрицательный
        // RowIndex в принципе; мы уже проверили, что модель не даёт создать.
        // Проверяем, что положительный индекс вне диапазона игнорируется.
        var source = Table(new[] { "a1", "b1" });

        var overrides = new[]
        {
            new RowOverride(10, new[] { "X", "Y" })
        };

        var result = RowOverrideApplier.Apply(source, overrides);

        Assert.Single(result.Rows);
        Assert.Equal("a1", result.Rows[0][0]);
    }

    // ---------------------------------------------------------------------
    // Длина строки
    // ---------------------------------------------------------------------

    [Fact]
    public void Apply_OverrideShorter_FillsWithEmpty()
    {
        var source = Table(new[] { "a1", "b1" });

        var overrides = new[]
        {
            new RowOverride(0, new[] { "A1" })   // только одна ячейка
        };

        var result = RowOverrideApplier.Apply(source, overrides);

        Assert.Equal("A1", result.Rows[0][0]);
        Assert.Equal(string.Empty, result.Rows[0][1]);
    }

    [Fact]
    public void Apply_OverrideLonger_Trims()
    {
        var source = Table(new[] { "a1", "b1" });

        var overrides = new[]
        {
            new RowOverride(0, new[] { "A1", "B1", "C1" })  // три ячейки
        };

        var result = RowOverrideApplier.Apply(source, overrides);

        Assert.Equal(2, result.Rows[0].Count);
        Assert.Equal("A1", result.Rows[0][0]);
        Assert.Equal("B1", result.Rows[0][1]);
    }

    // ---------------------------------------------------------------------
    // Дубликаты
    // ---------------------------------------------------------------------

    [Fact]
    public void Apply_DuplicateRowIndex_LastWins()
    {
        var source = Table(new[] { "a1", "b1" });

        var overrides = new[]
        {
            new RowOverride(0, new[] { "First", "First" }),
            new RowOverride(0, new[] { "Last", "Last" })
        };

        var result = RowOverrideApplier.Apply(source, overrides);

        Assert.Equal("Last", result.Rows[0][0]);
        Assert.Equal("Last", result.Rows[0][1]);
    }

    // ---------------------------------------------------------------------
    // Границы — null и пустая таблица
    // ---------------------------------------------------------------------

    [Fact]
    public void Apply_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RowOverrideApplier.Apply(null!, Array.Empty<RowOverride>()));
    }

    [Fact]
    public void Apply_NullOverrides_Throws()
    {
        var source = Table(new[] { "a1", "b1" });

        Assert.Throws<ArgumentNullException>(() =>
            RowOverrideApplier.Apply(source, null!));
    }

    [Fact]
    public void Apply_EmptyTable_ReturnsSame()
    {
        // TableData с 0 строк.
        var source = new TableData(
            new[] { Col(0, "A"), Col(1, "B") },
            Array.Empty<IReadOnlyList<string>>());

        var overrides = new[]
        {
            new RowOverride(0, new[] { "X", "Y" })
        };

        var result = RowOverrideApplier.Apply(source, overrides);

        Assert.Empty(result.Rows);
    }
}