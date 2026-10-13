using CertificateGenerator.Wpf.ViewModels;

namespace CertificateGenerator.Tests.ViewModels;

public class CellEditViewModelTests
{
    [Fact]
    public void Constructor_SetsOriginalAndValue()
    {
        var cell = new CellEditViewModel("Иванов");

        Assert.Equal("Иванов", cell.OriginalValue);
        Assert.Equal("Иванов", cell.Value);
    }

    [Fact]
    public void Constructor_NullOriginal_TreatedAsEmpty()
    {
        var cell = new CellEditViewModel(null);

        Assert.Equal(string.Empty, cell.OriginalValue);
        Assert.Equal(string.Empty, cell.Value);
    }

    [Fact]
    public void NewCell_IsNotDirty()
    {
        var cell = new CellEditViewModel("Иванов");

        Assert.False(cell.IsDirty);
    }

    [Fact]
    public void ChangeValue_MakesDirty()
    {
        var cell = new CellEditViewModel("Иванов");

        cell.Value = "Петров";

        Assert.True(cell.IsDirty);
        Assert.Equal("Петров", cell.Value);
        Assert.Equal("Иванов", cell.OriginalValue);
    }

    [Fact]
    public void ChangeValueBackToOriginal_NotDirty()
    {
        var cell = new CellEditViewModel("Иванов");

        cell.Value = "Петров";
        cell.Value = "Иванов";

        Assert.False(cell.IsDirty);
    }

    [Fact]
    public void ChangeToEmpty_IsDirty()
    {
        var cell = new CellEditViewModel("Иванов");

        cell.Value = "";

        Assert.True(cell.IsDirty);
    }

    [Fact]
    public void Reset_RestoresOriginal()
    {
        var cell = new CellEditViewModel("Иванов");
        cell.Value = "Петров";

        cell.Reset();

        Assert.Equal("Иванов", cell.Value);
        Assert.False(cell.IsDirty);
    }

    [Fact]
    public void ChangeValue_RaisesPropertyChangedForValueAndIsDirty()
    {
        var cell = new CellEditViewModel("Иванов");
        var changedProperties = new List<string?>();
        cell.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        cell.Value = "Петров";

        Assert.Contains("Value", changedProperties);
        Assert.Contains("IsDirty", changedProperties);
    }

    [Fact]
    public void SetSameValue_DoesNotRaisePropertyChanged()
    {
        var cell = new CellEditViewModel("Иванов");
        var changedProperties = new List<string?>();
        cell.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        // То же самое значение — CommunityToolkit должен пропустить событие.
        cell.Value = "Иванов";

        Assert.Empty(changedProperties);
    }

    [Fact]
    public void Reset_WhenAlreadyClean_DoesNotRaisePropertyChanged()
    {
        var cell = new CellEditViewModel("Иванов");
        var changedProperties = new List<string?>();
        cell.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        cell.Reset();

        Assert.Empty(changedProperties);
    }

        // ---------------------------------------------------------------------
    // ValueChanged
    // ---------------------------------------------------------------------

    [Fact]
    public void ChangeValue_RaisesValueChanged()
    {
        var cell = new CellEditViewModel("Иванов");
        var raised = 0;
        cell.ValueChanged += (_, _) => raised++;

        cell.Value = "Петров";

        Assert.Equal(1, raised);
    }

    [Fact]
    public void SetSameValue_DoesNotRaiseValueChanged()
    {
        var cell = new CellEditViewModel("Иванов");
        var raised = 0;
        cell.ValueChanged += (_, _) => raised++;

        cell.Value = "Иванов";   // то же самое

        Assert.Equal(0, raised);
    }

    [Fact]
    public void ChangeValueTwice_RaisesTwice()
    {
        var cell = new CellEditViewModel("Иванов");
        var raised = 0;
        cell.ValueChanged += (_, _) => raised++;

        cell.Value = "Петров";
        cell.Value = "Сидоров";

        Assert.Equal(2, raised);
    }

    [Fact]
    public void Reset_WhenDirty_RaisesValueChanged()
    {
        var cell = new CellEditViewModel("Иванов");
        cell.Value = "Петров";

        var raised = 0;
        cell.ValueChanged += (_, _) => raised++;

        cell.Reset();

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Reset_WhenClean_DoesNotRaiseValueChanged()
    {
        var cell = new CellEditViewModel("Иванов");
        var raised = 0;
        cell.ValueChanged += (_, _) => raised++;

        cell.Reset();

        Assert.Equal(0, raised);
    }
}