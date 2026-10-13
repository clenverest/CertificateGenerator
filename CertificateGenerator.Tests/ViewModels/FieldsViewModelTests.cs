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

    private static ColumnInfo Col(int index, string name) => new(index, name);

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
        Assert.Empty(vm.Columns);
        Assert.False(vm.HasColumns);
    }

    [Fact]
    public void SetColumns_StoresAndUpdatesHasColumns()
    {
        var vm = CreateViewModel();
        var columns = new[] { Col(0, "ФИО"), Col(1, "Класс") };

        vm.SetColumns(columns);

        Assert.Equal(2, vm.Columns.Count);
        Assert.True(vm.HasColumns);
    }

    [Fact]
    public void SetColumns_Null_TreatedAsEmpty()
    {
        var vm = CreateViewModel();

        vm.SetColumns(null!);

        Assert.Empty(vm.Columns);
        Assert.False(vm.HasColumns);
    }

    [Fact]
    public void AddField_DialogReturnsNull_DoesNothing()
    {
        var dialog = new FakeFieldEditDialogService { FieldToReturnOnCreate = null };
        var vm = CreateViewModel(dialog);
        vm.SetColumns(new[] { Col(0, "ФИО") });   // ← добавить

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
        vm.SetColumns(new[] { Col(0, "ФИО") });   // ← добавить

        vm.AddFieldCommand.Execute(null);

        Assert.Single(vm.Fields);
        Assert.Same(created, vm.Fields[0]);
        Assert.Same(created, vm.SelectedField);
    }

    [Fact]
    public void AddField_PassesExistingNamesAndColumnsToDialog()
    {
        var dialog = new FakeFieldEditDialogService();
        var vm = CreateViewModel(dialog);
        vm.LoadFields(new[] { MakeSimple("Класс"), MakeSimple("Школа") });
        vm.SetColumns(new[] { Col(0, "ФИО"), Col(1, "Класс") });

        vm.AddFieldCommand.Execute(null);

        var call = dialog.ShowCreateCalls[0];
        Assert.Equal(2, call.ExistingNames.Count);
        Assert.Contains("Класс", call.ExistingNames);
        Assert.Contains("Школа", call.ExistingNames);
        Assert.Equal(2, call.Columns.Count);
        Assert.Equal("ФИО", call.Columns[0].Name);
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

        var call = dialog.ShowEditCalls[0];
        Assert.Single(call.OtherNames);
        Assert.Contains("Школа", call.OtherNames);
        Assert.DoesNotContain("Класс", call.OtherNames);
    }

    [Fact]
    public void EditField_PassesColumnsToDialog()
    {
        var field = MakeSimple("Класс");
        var dialog = new FakeFieldEditDialogService();
        var vm = CreateViewModel(dialog);
        vm.LoadFields(new[] { field });
        vm.SelectedField = field;
        vm.SetColumns(new[] { Col(0, "Класс"), Col(1, "Школа") });

        vm.EditFieldCommand.Execute(null);

        var call = dialog.ShowEditCalls[0];
        Assert.Equal(2, call.Columns.Count);
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
        vm.SetColumns(new[] { Col(0, "ФИО") });   // ← добавить
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

        [Fact]
    public void AddField_WithoutColumns_CanExecuteIsFalse()
    {
        var vm = CreateViewModel();

        Assert.False(vm.AddFieldCommand.CanExecute(null));
    }

    [Fact]
    public void AddField_WithColumns_CanExecuteIsTrue()
    {
        var vm = CreateViewModel();
        vm.SetColumns(new[] { Col(0, "ФИО") });

        Assert.True(vm.AddFieldCommand.CanExecute(null));
    }

    [Fact]
    public void SetColumns_FromEmptyToFilled_EnablesAddField()
    {
        var vm = CreateViewModel();
        Assert.False(vm.AddFieldCommand.CanExecute(null));

        vm.SetColumns(new[] { Col(0, "ФИО") });

        Assert.True(vm.AddFieldCommand.CanExecute(null));
    }
}