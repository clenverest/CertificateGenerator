using CertificateGenerator.Wpf.ViewModels;

namespace CertificateGenerator.Tests.ViewModels;

public class RowEditViewModelTests
{
    [Fact]
    public void Constructor_CreatesCellsForEachValue()
    {
        var row = new RowEditViewModel(0, new[] { "Иванов", "5А", "Москва" });

        Assert.Equal(3, row.Cells.Count);
        Assert.Equal("Иванов", row.Cells[0].Value);
        Assert.Equal("5А", row.Cells[1].Value);
        Assert.Equal("Москва", row.Cells[2].Value);
    }

    [Fact]
    public void Constructor_SetsRowIndex()
    {
        var row = new RowEditViewModel(5, new[] { "Иванов" });

        Assert.Equal(5, row.RowIndex);
    }

    [Fact]
    public void Constructor_EmptyValues_CreatesEmptyCells()
    {
        var row = new RowEditViewModel(0, Array.Empty<string>());

        Assert.Empty(row.Cells);
        Assert.False(row.IsDirty);
    }

    [Fact]
    public void Constructor_NullValues_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RowEditViewModel(0, null!));
    }

    [Fact]
    public void NewRow_IsNotDirty()
    {
        var row = new RowEditViewModel(0, new[] { "Иванов", "5А" });

        Assert.False(row.IsDirty);
    }

    [Fact]
    public void ChangeOneCell_MakesRowDirty()
    {
        var row = new RowEditViewModel(0, new[] { "Иванов", "5А" });

        row.Cells[0].Value = "Петров";

        Assert.True(row.IsDirty);
    }

    [Fact]
    public void ChangeCellBackToOriginal_RowNotDirty()
    {
        var row = new RowEditViewModel(0, new[] { "Иванов", "5А" });

        row.Cells[0].Value = "Петров";
        row.Cells[0].Value = "Иванов";

        Assert.False(row.IsDirty);
    }

    [Fact]
    public void ChangeMultipleCells_RowDirty()
    {
        var row = new RowEditViewModel(0, new[] { "Иванов", "5А" });

        row.Cells[0].Value = "Петров";
        row.Cells[1].Value = "6Б";

        Assert.True(row.IsDirty);
    }

    [Fact]
    public void ResetAll_WhenDirty_ClearsDirty()
    {
        var row = new RowEditViewModel(0, new[] { "Иванов", "5А" });
        row.Cells[0].Value = "Петров";
        row.Cells[1].Value = "6Б";

        row.Reset();

        Assert.False(row.IsDirty);
        Assert.Equal("Иванов", row.Cells[0].Value);
        Assert.Equal("5А", row.Cells[1].Value);
    }

    [Fact]
    public void ChangeCell_RaisesPropertyChangedForIsDirty()
    {
        var row = new RowEditViewModel(0, new[] { "Иванов", "5А" });
        var changedProperties = new List<string?>();
        row.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        row.Cells[0].Value = "Петров";

        Assert.Contains("IsDirty", changedProperties);
    }

    [Fact]
    public void ChangeCellBack_RaisesIsDirtyTwice()
    {
        var row = new RowEditViewModel(0, new[] { "Иванов", "5А" });
        var isDirtyRaises = 0;
        row.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == "IsDirty")
            {
                isDirtyRaises++;
            }
        };

        row.Cells[0].Value = "Петров";   // IsDirty: false → true
        row.Cells[0].Value = "Иванов";   // IsDirty: true → false

        Assert.Equal(2, isDirtyRaises);
    }

    [Fact]
    public void ChangeValueInsideAlreadyDirtyCell_RaisesRowIsDirtyOnce()
    {
        var row = new RowEditViewModel(0, new[] { "Иванов", "5А" });
        row.Cells[0].Value = "Петров";   // уже грязная
        var isDirtyRaises = 0;
        row.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == "IsDirty")
            {
                isDirtyRaises++;
            }
        };

        row.Cells[0].Value = "Сидоров";   // всё ещё грязная

        // Ячейка поднимает IsDirty, строка тоже поднимает — но значение не изменилось.
        // Мы не фильтруем по фактическому изменению значения — просто пересчитываем.
        Assert.Equal(1, isDirtyRaises);
    }
}