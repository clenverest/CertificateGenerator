using CertificateGenerator.Core.Models;
using CertificateGenerator.Tests.Fakes;
using CertificateGenerator.Wpf.ViewModels;

namespace CertificateGenerator.Tests.ViewModels;

public class FieldsViewModelTests
{
    private static Field MakeSimple(string name = "Класс")
    {
        return new Field(name, FieldKind.Simple, new[] { 0 });
    }

    private static FieldsViewModel CreateViewModel(
        FakeFieldEditDialogService? dialogService = null)
    {
        return new FieldsViewModel(dialogService ?? new FakeFieldEditDialogService());
    }

    [Fact]
    public void NewViewModel_IsEmpty()
    {
        var vm = CreateViewModel();

        Assert.Empty(vm.Fields);
        Assert.Null(vm.SelectedField);
    }

    [Fact]
    public void AddField_DialogReturnsNull_DoesNothing()
    {
        var dialog = new FakeFieldEditDialogService { FieldToReturnOnCreate = null };
        var vm = CreateViewModel(dialog);

        vm.AddFieldCommand.Execute(null);

        Assert.Empty(vm.Fields);
        Assert.Single(dialog.ShowCreateCalls);
    }

    [Fact]
    public void AddField_DialogReturnsField_AddsAndSelects()
    {
        var created = MakeSimple("Школа");
        var dialog = new FakeFieldEditDialogService { FieldToReturnOnCreate = created };
        var vm = CreateViewModel(dialog);

        vm.AddFieldCommand.Execute(null);

        Assert.Single(vm.Fields);
        Assert.Same(created, vm.Fields[0]);
        Assert.Same(created, vm.SelectedField);
    }

    [Fact]
    public void AddField_PassesExistingNamesToDialog()
    {
        var dialog = new FakeFieldEditDialogService();
        var vm = CreateViewModel(dialog);
        vm.LoadFields(new[] { MakeSimple("Класс"), MakeSimple("Школа") });

        vm.AddFieldCommand.Execute(null);

        var passedNames = dialog.ShowCreateCalls[0];
        Assert.Equal(2, passedNames.Count);
        Assert.Contains("Класс", passedNames);
        Assert.Contains("Школа", passedNames);
    }

    [Fact]
    public void EditField_WithoutSelection_CanExecuteIsFalse()
    {
        var vm = CreateViewModel();

        Assert.False(vm.EditFieldCommand.CanExecute(null));
    }

    [Fact]
    public void EditField_WithSelection_CanExecuteIsTrue()
    {
        var vm = CreateViewModel();
        vm.LoadFields(new[] { MakeSimple() });
        vm.SelectedField = vm.Fields[0];

        Assert.True(vm.EditFieldCommand.CanExecute(null));
    }

    [Fact]
    public void EditField_DialogReturnsNull_KeepsOriginal()
    {
        var original = MakeSimple("Класс");
        var dialog = new FakeFieldEditDialogService { FieldToReturnOnEdit = null };
        var vm = CreateViewModel(dialog);
        vm.LoadFields(new[] { original });
        vm.SelectedField = original;

        vm.EditFieldCommand.Execute(null);

        Assert.Single(vm.Fields);
        Assert.Same(original, vm.Fields[0]);
        Assert.Same(original, vm.SelectedField);
    }

    [Fact]
    public void EditField_DialogReturnsUpdated_ReplacesInCollection()
    {
        var original = MakeSimple("Класс");
        var updated = MakeSimple("Класс (обновлено)");
        var dialog = new FakeFieldEditDialogService { FieldToReturnOnEdit = updated };
        var vm = CreateViewModel(dialog);
        vm.LoadFields(new[] { original });
        vm.SelectedField = original;

        vm.EditFieldCommand.Execute(null);

        Assert.Single(vm.Fields);
        Assert.Same(updated, vm.Fields[0]);
        Assert.Same(updated, vm.SelectedField);
    }

    [Fact]
    public void EditField_ExcludesSelfFromExistingNames()
    {
        var first = MakeSimple("Класс");
        var second = MakeSimple("Школа");
        var dialog = new FakeFieldEditDialogService();
        var vm = CreateViewModel(dialog);
        vm.LoadFields(new[] { first, second });
        vm.SelectedField = first;

        vm.EditFieldCommand.Execute(null);

        var (_, otherNames) = dialog.ShowEditCalls[0];
        Assert.Single(otherNames);
        Assert.Contains("Школа", otherNames);
        Assert.DoesNotContain("Класс", otherNames);
    }

    [Fact]
    public void DeleteField_WithoutSelection_CanExecuteIsFalse()
    {
        var vm = CreateViewModel();

        Assert.False(vm.DeleteFieldCommand.CanExecute(null));
    }

    [Fact]
    public void LoadFields_ReplacesContent()
    {
        var vm = CreateViewModel();
        vm.AddFieldCommand.Execute(null);

        var newFields = new[]
        {
            MakeSimple("Класс"),
            new Field("ФИО", FieldKind.FullName, new[] { 0, 1 })
        };
        vm.LoadFields(newFields);

        Assert.Equal(2, vm.Fields.Count);
        Assert.Null(vm.SelectedField);
    }

    [Fact]
    public void GetFieldsSnapshot_ReturnsCurrentFields()
    {
        var vm = CreateViewModel();
        vm.LoadFields(new[] { MakeSimple("Класс"), MakeSimple("Школа") });

        var snapshot = vm.GetFieldsSnapshot();

        Assert.Equal(2, snapshot.Count);
        Assert.Equal("Класс", snapshot[0].Name);
        Assert.Equal("Школа", snapshot[1].Name);
    }

    [Fact]
    public void ExistingNames_ReflectsFields()
    {
        var vm = CreateViewModel();
        vm.LoadFields(new[] { MakeSimple("Класс"), MakeSimple("Школа") });

        var names = vm.ExistingNames;

        Assert.Equal(2, names.Count);
        Assert.Contains("Класс", names);
        Assert.Contains("Школа", names);
    }
}