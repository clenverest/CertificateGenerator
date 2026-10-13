using CertificateGenerator.Wpf.ViewModels;

namespace CertificateGenerator.Tests.ViewModels;

public class ValidationRowViewModelTests
{
    private static ValidationCellViewModel Cell(
        string fieldName = "ФИО",
        string value = "Иванов",
        bool hasError = false,
        bool hasWarning = false)
    {
        return new ValidationCellViewModel(fieldName, value, hasError, hasWarning);
    }

    [Fact]
    public void Constructor_StoresRowIndexAndCells()
    {
        var cells = new[] { Cell(), Cell("Класс", "5А") };

        var row = new ValidationRowViewModel(3, cells);

        Assert.Equal(3, row.RowIndex);
        Assert.Equal(2, row.Cells.Count);
    }

    [Fact]
    public void Constructor_NullCells_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ValidationRowViewModel(0, null!));
    }

    [Fact]
    public void DisplayRowNumber_IsRowIndexPlusOne()
    {
        var row = new ValidationRowViewModel(0, new[] { Cell() });

        Assert.Equal(1, row.DisplayRowNumber);
    }

    [Fact]
    public void DisplayRowNumber_ForRow5_Returns6()
    {
        var row = new ValidationRowViewModel(5, new[] { Cell() });

        Assert.Equal(6, row.DisplayRowNumber);
    }

    // ---------------------------------------------------------------------
    // HasError
    // ---------------------------------------------------------------------

    [Fact]
    public void HasError_NoErrors_False()
    {
        var row = new ValidationRowViewModel(0, new[] { Cell(), Cell() });

        Assert.False(row.HasError);
    }

    [Fact]
    public void HasError_OneCellWithError_True()
    {
        var cells = new[]
        {
            Cell(),
            Cell("Класс", "", hasError: true)
        };

        var row = new ValidationRowViewModel(0, cells);

        Assert.True(row.HasError);
    }

    // ---------------------------------------------------------------------
    // HasWarning
    // ---------------------------------------------------------------------

    [Fact]
    public void HasWarning_NoWarnings_False()
    {
        var row = new ValidationRowViewModel(0, new[] { Cell(), Cell() });

        Assert.False(row.HasWarning);
    }

    [Fact]
    public void HasWarning_OneWarning_True()
    {
        var cells = new[]
        {
            Cell(),
            Cell("Класс", "5А", hasWarning: true)
        };

        var row = new ValidationRowViewModel(0, cells);

        Assert.True(row.HasWarning);
    }

    [Fact]
    public void HasWarning_ErrorTakesPriority()
    {
        // Если есть и ошибка, и предупреждение — HasWarning = false.
        var cells = new[]
        {
            Cell("ФИО", "", hasError: true),
            Cell("Класс", "5А", hasWarning: true)
        };

        var row = new ValidationRowViewModel(0, cells);

        Assert.True(row.HasError);
        Assert.False(row.HasWarning);
    }

    [Fact]
    public void HasWarning_OnlyWarnings_True()
    {
        var cells = new[]
        {
            Cell("ФИО", "Иванов", hasWarning: true),
            Cell("Класс", "5А", hasWarning: true)
        };

        var row = new ValidationRowViewModel(0, cells);

        Assert.False(row.HasError);
        Assert.True(row.HasWarning);
    }

    [Fact]
    public void EmptyCells_NoErrorNoWarning()
    {
        var row = new ValidationRowViewModel(0, Array.Empty<ValidationCellViewModel>());

        Assert.False(row.HasError);
        Assert.False(row.HasWarning);
    }
}