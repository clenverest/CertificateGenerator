using CertificateGenerator.Core.Models;
using CertificateGenerator.Wpf.ViewModels;

namespace CertificateGenerator.Tests.ViewModels;

public sealed class FieldPlacementViewModelTests
{
    private static FieldPlacement Sample() =>
        new("ФИО", 100, 200, 48, "Arial", FieldAlignment.Center);

    [Fact]
    public void Constructor_AllValuesCopied()
    {
        var vm = new FieldPlacementViewModel(Sample());

        Assert.Equal("ФИО", vm.FieldName);
        Assert.Equal(100, vm.X);
        Assert.Equal(200, vm.Y);
        Assert.Equal(48, vm.FontSize);
        Assert.Equal("Arial", vm.FontFamily);
        Assert.Equal(FieldAlignment.Center, vm.Alignment);
    }

    [Fact]
    public void Constructor_NullModel_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FieldPlacementViewModel(null!));
    }

    [Fact]
    public void Constructor_DoesNotRaiseChanged()
    {
        var vm = new FieldPlacementViewModel(Sample());

        var raised = false;
        vm.Changed += (_, _) => raised = true;

        // Никаких изменений не было — событие не должно сработать.
        Assert.False(raised);
    }

    [Fact]
    public void SetX_UpdatesModelAndRaisesChanged()
    {
        var vm = new FieldPlacementViewModel(Sample());
        var raised = false;
        vm.Changed += (_, _) => raised = true;

        vm.X = 500;

        Assert.Equal(500, vm.X);
        Assert.Equal(500, vm.ToModel().X);
        Assert.Equal(200, vm.ToModel().Y);  // Y не изменился
        Assert.True(raised);
    }

    [Fact]
    public void SetY_UpdatesModelAndRaisesChanged()
    {
        var vm = new FieldPlacementViewModel(Sample());
        var raised = false;
        vm.Changed += (_, _) => raised = true;

        vm.Y = 999;

        Assert.Equal(999, vm.Y);
        Assert.Equal(999, vm.ToModel().Y);
        Assert.True(raised);
    }

    [Fact]
    public void SetFontSize_UpdatesModel()
    {
        var vm = new FieldPlacementViewModel(Sample());

        vm.FontSize = 72;

        Assert.Equal(72, vm.ToModel().FontSize);
    }

    [Fact]
    public void SetFontFamily_UpdatesModel()
    {
        var vm = new FieldPlacementViewModel(Sample());

        vm.FontFamily = "Times New Roman";

        Assert.Equal("Times New Roman", vm.ToModel().FontFamily);
    }

    [Fact]
    public void SetAlignment_UpdatesModel()
    {
        var vm = new FieldPlacementViewModel(Sample());

        vm.Alignment = FieldAlignment.Right;

        Assert.Equal(FieldAlignment.Right, vm.ToModel().Alignment);
    }

    [Fact]
    public void SetSameValue_DoesNotRaiseChanged()
    {
        var vm = new FieldPlacementViewModel(Sample());
        var raised = false;
        vm.Changed += (_, _) => raised = true;

        vm.X = 100;  // то же значение

        Assert.False(raised);
    }
}