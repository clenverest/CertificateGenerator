using CertificateGenerator.Core.Models;
using CertificateGenerator.Tests.Fakes;
using CertificateGenerator.Wpf.ViewModels;

namespace CertificateGenerator.Tests.ViewModels;

public sealed class MainViewModelTests
{
    private static TableData EmptyTable()
    {
        return new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            Array.Empty<IReadOnlyList<string>>());
    }

    private static MainViewModel CreateViewModel()
    {
        return new MainViewModel(
            new FakeTableReaderResolver(new FakeTableReader(EmptyTable())),
            new FakeProjectStorage(),
            new FakeWindowNavigator(),
            new FieldsViewModel(new FakeFieldEditDialogService()));
    }

    [Fact]
    public void Constructor_InitialState_HasNoProject()
    {
        var vm = CreateViewModel();

        Assert.Null(vm.CurrentProject);
        Assert.Null(vm.ProjectFolderPath);
        Assert.Null(vm.TableData);
        Assert.Null(vm.ErrorMessage);
        Assert.False(vm.HasError);
    }

    [Fact]
    public void Initialize_SetsProjectAndPath()
    {
        var vm = CreateViewModel();
        var project = new Project(
            "Осень 2026", DateTime.Now, "data.xlsx", Array.Empty<Field>());

        vm.Initialize(project, @"C:\Projects\Осень 2026");

        Assert.NotNull(vm.CurrentProject);
        Assert.Equal("Осень 2026", vm.CurrentProject!.Name);
        Assert.Equal(@"C:\Projects\Осень 2026", vm.ProjectFolderPath);
        Assert.True(vm.HasProject);
        Assert.Equal("Осень 2026", vm.ProjectName);
        Assert.Equal(@"C:\Projects\Осень 2026", vm.ProjectPath);
    }

    [Fact]
    public void LoadTableIntoProjectFromPath_ValidPath_CopiesAndReadsTable()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов Иван" } });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = new MainViewModel(
            resolver, storage, new FakeWindowNavigator(), new FieldsViewModel(new FakeFieldEditDialogService()));

        var project = new Project("A", DateTime.Now, string.Empty, Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");

        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.NotNull(vm.TableData);
        Assert.Single(vm.TableData!.Rows);
        Assert.Equal(@"C:\Projects\A\source\data.xlsx", vm.SelectedFilePath);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public void LoadTableIntoProjectFromPath_NoProject_ShowsError()
    {
        var storage = new FakeProjectStorage();
        var resolver = new FakeTableReaderResolver(new FakeTableReader(EmptyTable()));
        var vm = new MainViewModel(
            resolver, storage, new FakeWindowNavigator(), new FieldsViewModel(new FakeFieldEditDialogService()));

        // Не вызываем Initialize — CurrentProject остаётся null.
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.Equal("Проект не открыт.", vm.ErrorMessage);
        Assert.Null(vm.TableData);
    }

    [Fact]
    public void LoadTableIntoProjectFromPath_StorageThrows_SetsErrorMessage()
    {
        var storage = new FakeProjectStorage
        {
            ExceptionToThrowOnSetSourceTable =
                new IOException("Файл занят другим процессом.")
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(EmptyTable()));
        var vm = new MainViewModel(
            resolver, storage, new FakeWindowNavigator(), new FieldsViewModel(new FakeFieldEditDialogService()));

        var project = new Project("A", DateTime.Now, string.Empty, Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");

        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.Equal("Файл занят другим процессом.", vm.ErrorMessage);
        Assert.Null(vm.TableData);
    }

    [Fact]
    public void Initialize_ProjectWithSourceTable_LoadsTable()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Петров Пётр" } });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = new MainViewModel(
            resolver, storage, new FakeWindowNavigator(), new FieldsViewModel(new FakeFieldEditDialogService()));

        var project = new Project("A", DateTime.Now, "data.xlsx", Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");

        Assert.NotNull(vm.TableData);
        Assert.Single(vm.TableData!.Rows);
        Assert.Equal(@"C:\Projects\A\source\data.xlsx", vm.SelectedFilePath);
    }

    // ---------------------------------------------------------------------
    // Поля проекта
    // ---------------------------------------------------------------------

    [Fact]
    public void Initialize_WithFields_LoadsThemIntoFieldsViewModel()
    {
        var vm = CreateViewModel();
        var fields = new[]
        {
            new Field("Класс", FieldKind.Simple, new[] { 0 }),
            new Field("ФИО", FieldKind.FullName, new[] { 1, 2 })
        };
        var project = new Project("A", DateTime.Now, string.Empty, fields);

        vm.Initialize(project, @"C:\Projects\A");

        Assert.Equal(2, vm.Fields.Fields.Count);
        Assert.Equal("Класс", vm.Fields.Fields[0].Name);
        Assert.Equal("ФИО", vm.Fields.Fields[1].Name);
    }

    [Fact]
    public void Initialize_DoesNotSaveProjectToStorage()
    {
        var storage = new FakeProjectStorage();
        var vm = new MainViewModel(
            new FakeTableReaderResolver(new FakeTableReader(EmptyTable())),
            storage,
            new FakeWindowNavigator(),
            new FieldsViewModel(new FakeFieldEditDialogService()));

        var fields = new[] { new Field("Класс", FieldKind.Simple, new[] { 0 }) };
        var project = new Project("A", DateTime.Now, string.Empty, fields);

        vm.Initialize(project, @"C:\Projects\A");

        Assert.Empty(storage.SaveCalls);
    }

    [Fact]
    public void AddField_SavesUpdatedProjectToStorage()
    {
        var storage = new FakeProjectStorage();

        var dialog = new FakeFieldEditDialogService
        {
            FieldToReturnOnCreate = new Field("Класс", FieldKind.Simple, new[] { 0 })
        };

        var vm = new MainViewModel(
            new FakeTableReaderResolver(new FakeTableReader(EmptyTable())),
            storage,
            new FakeWindowNavigator(),
            new FieldsViewModel(dialog));

        var project = new Project("A", DateTime.Now, string.Empty, Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");

        vm.Fields.AddFieldCommand.Execute(null);

        Assert.Single(storage.SaveCalls);
        Assert.Equal(@"C:\Projects\A", storage.SaveCalls[0].ProjectFolderPath);
        Assert.Single(storage.SaveCalls[0].Project.Fields);
        Assert.Single(vm.CurrentProject!.Fields);
    }

    [Fact]
    public void AddField_PreservesProjectNameCreatedAtSourceFileName()
    {
        var storage = new FakeProjectStorage();
        var dialog = new FakeFieldEditDialogService
        {
            FieldToReturnOnCreate = new Field("Класс", FieldKind.Simple, new[] { 0 })
        };
        var vm = new MainViewModel(
            new FakeTableReaderResolver(new FakeTableReader(EmptyTable())),
            storage,
            new FakeWindowNavigator(),
            new FieldsViewModel(dialog));

        var createdAt = new DateTime(2026, 1, 1);
        var project = new Project("A", createdAt, "data.xlsx", Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");

        vm.Fields.AddFieldCommand.Execute(null);

        var saved = storage.SaveCalls[0].Project;
        Assert.Equal("A", saved.Name);
        Assert.Equal(createdAt, saved.CreatedAt);
        Assert.Equal("data.xlsx", saved.SourceFileName);
    }

    [Fact]
    public void AddField_SaveThrows_SetsErrorMessage()
    {
        var storage = new FakeProjectStorage
        {
            ExceptionToThrowOnSave = new IOException("Диск недоступен.")
        };

        var dialog = new FakeFieldEditDialogService
        {
            FieldToReturnOnCreate = new Field("Класс", FieldKind.Simple, new[] { 0 })
        };

        var vm = new MainViewModel(
            new FakeTableReaderResolver(new FakeTableReader(EmptyTable())),
            storage,
            new FakeWindowNavigator(),
            new FieldsViewModel(dialog));

        var project = new Project("A", DateTime.Now, string.Empty, Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");

        vm.Fields.AddFieldCommand.Execute(null);

        Assert.NotNull(vm.ErrorMessage);
        Assert.Contains("Диск недоступен", vm.ErrorMessage!);
        // CurrentProject не должен обновиться.
        Assert.Empty(vm.CurrentProject!.Fields);
    }
}