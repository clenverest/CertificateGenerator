using CertificateGenerator.Wpf.ViewModels;

namespace CertificateGenerator.Tests.ViewModels;

public class ValidationCellViewModelTests
{
    [Fact]
    public void Constructor_StoresValues()
    {
        var cell = new ValidationCellViewModel("ФИО", "Иванов Иван", false, false);

        Assert.Equal("ФИО", cell.FieldName);
        Assert.Equal("Иванов Иван", cell.Value);
        Assert.False(cell.HasError);
        Assert.False(cell.HasWarning);
    }

    [Fact]
    public void Constructor_HasError_Stored()
    {
        var cell = new ValidationCellViewModel("ФИО", "", true, false);

        Assert.True(cell.HasError);
        Assert.False(cell.HasWarning);
    }

    [Fact]
    public void Constructor_HasWarning_Stored()
    {
        var cell = new ValidationCellViewModel("ФИО", "Иванов", false, true);

        Assert.False(cell.HasError);
        Assert.True(cell.HasWarning);
    }

    [Fact]
    public void Constructor_BothFlags_Stored()
    {
        var cell = new ValidationCellViewModel("ФИО", "  ", true, true);

        Assert.True(cell.HasError);
        Assert.True(cell.HasWarning);
    }

    [Fact]
    public void Constructor_NullFieldName_TreatedAsEmpty()
    {
        var cell = new ValidationCellViewModel(null!, "value", false, false);

        Assert.Equal(string.Empty, cell.FieldName);
    }

    [Fact]
    public void Constructor_NullValue_TreatedAsEmpty()
    {
        var cell = new ValidationCellViewModel("ФИО", null!, false, false);

        Assert.Equal(string.Empty, cell.Value);
    }

    // ---------------------------------------------------------------------
    // IsEmpty
    // ---------------------------------------------------------------------

    [Fact]
    public void IsEmpty_EmptyString_True()
    {
        var cell = new ValidationCellViewModel("ФИО", "", false, false);

        Assert.True(cell.IsEmpty);
    }

    [Fact]
    public void IsEmpty_WhitespaceOnly_True()
    {
        var cell = new ValidationCellViewModel("ФИО", "   ", false, false);

        Assert.True(cell.IsEmpty);
    }

    [Fact]
    public void IsEmpty_ValueWithText_False()
    {
        var cell = new ValidationCellViewModel("ФИО", "Иванов", false, false);

        Assert.False(cell.IsEmpty);
    }

    [Fact]
    public void IsEmpty_ValueWithSurroundingWhitespace_False()
    {
        var cell = new ValidationCellViewModel("ФИО", "  Иванов  ", false, false);

        Assert.False(cell.IsEmpty);
    }
}