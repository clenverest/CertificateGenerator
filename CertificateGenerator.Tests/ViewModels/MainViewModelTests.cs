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
        vm.Fields.SetColumns(new[] { new ColumnInfo(0, "ФИО") }); 

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
        vm.Fields.SetColumns(new[] { new ColumnInfo(0, "ФИО") }); 

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
        vm.Fields.SetColumns(new[] { new ColumnInfo(0, "ФИО") }); 

        vm.Fields.AddFieldCommand.Execute(null);

        Assert.NotNull(vm.ErrorMessage);
        Assert.Contains("Диск недоступен", vm.ErrorMessage!);
        // CurrentProject не должен обновиться.
        Assert.Empty(vm.CurrentProject!.Fields);
    }

    [Fact]
    public void LoadTableIntoProjectFromPath_UpdatesFieldsColumns()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО"), new ColumnInfo(1, "Класс") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов Иван", "5А" } });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = new MainViewModel(
            resolver, storage, new FakeWindowNavigator(),
            new FieldsViewModel(new FakeFieldEditDialogService()));

        var project = new Project("A", DateTime.Now, string.Empty, Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");

        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.Equal(2, vm.Fields.Columns.Count);
        Assert.True(vm.Fields.HasColumns);
    }

    // ---------------------------------------------------------------------
    // FieldPreviews
    // ---------------------------------------------------------------------

    [Fact]
    public void Initialize_WithoutTable_HasNoPreviews()
    {
        var vm = CreateViewModel();
        var fields = new[] { new Field("Класс", FieldKind.Simple, new[] { 0 }) };
        var project = new Project("A", DateTime.Now, string.Empty, fields);

        vm.Initialize(project, @"C:\Projects\A");

        Assert.Empty(vm.FieldPreviews);
        Assert.False(vm.HasPreviews);
    }

    [Fact]
    public void LoadTableIntoProjectFromPath_FillsPreviews()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "Класс") },
            new[] { (IReadOnlyList<string>)new[] { "5А" } });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = new MainViewModel(
            resolver, storage, new FakeWindowNavigator(),
            new FieldsViewModel(new FakeFieldEditDialogService()));

        var fields = new[] { new Field("Класс", FieldKind.Simple, new[] { 0 }) };
        var project = new Project("A", DateTime.Now, string.Empty, fields);
        vm.Initialize(project, @"C:\Projects\A");

        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.Single(vm.FieldPreviews);
        Assert.Equal("Класс", vm.FieldPreviews[0].FieldName);
        Assert.Equal("5А", vm.FieldPreviews[0].PreviewValue);
        Assert.True(vm.HasPreviews);
    }

    [Fact]
    public void UpdateFieldPreviews_FullName_JoinsColumns()
    {
        var table = new TableData(
            new[]
            {
                new ColumnInfo(0, "Фамилия"),
                new ColumnInfo(1, "Имя"),
                new ColumnInfo(2, "Отчество")
            },
            new[]
            {
                (IReadOnlyList<string>)new[] { "Иванов", "Иван", "Иванович" }
            });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = new MainViewModel(
            resolver, storage, new FakeWindowNavigator(),
            new FieldsViewModel(new FakeFieldEditDialogService()));

        var fields = new[] { new Field("ФИО", FieldKind.FullName, new[] { 0, 1, 2 }) };
        var project = new Project("A", DateTime.Now, string.Empty, fields);
        vm.Initialize(project, @"C:\Projects\A");

        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.Single(vm.FieldPreviews);
        Assert.Equal("ФИО", vm.FieldPreviews[0].FieldName);
        Assert.Equal("Иванов Иван Иванович", vm.FieldPreviews[0].PreviewValue);
    }

        // ---------------------------------------------------------------------
    // Rows (редактируемые строки)
    // ---------------------------------------------------------------------

    [Fact]
    public void Initialize_WithoutTable_RowsIsEmpty()
    {
        var vm = CreateViewModel();
        var project = new Project("A", DateTime.Now, string.Empty, Array.Empty<Field>());

        vm.Initialize(project, @"C:\Projects\A");

        Assert.Empty(vm.Rows);
    }

    [Fact]
    public void LoadTableIntoProjectFromPath_BuildsRows()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО"), new ColumnInfo(1, "Класс") },
            new[]
            {
                (IReadOnlyList<string>)new[] { "Иванов", "5А" },
                (IReadOnlyList<string>)new[] { "Петров", "6Б" }
            });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = new MainViewModel(
            resolver, storage, new FakeWindowNavigator(),
            new FieldsViewModel(new FakeFieldEditDialogService()));

        var project = new Project("A", DateTime.Now, string.Empty, Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");

        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.Equal(2, vm.Rows.Count);
        Assert.Equal(0, vm.Rows[0].RowIndex);
        Assert.Equal(1, vm.Rows[1].RowIndex);
        Assert.Equal(2, vm.Rows[0].Cells.Count);
        Assert.Equal("Иванов", vm.Rows[0].Cells[0].Value);
        Assert.Equal("5А", vm.Rows[0].Cells[1].Value);
    }

    [Fact]
    public void LoadNewTable_ReplacesRows()
    {
        var firstTable = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(firstTable));
        var vm = new MainViewModel(
            resolver, storage, new FakeWindowNavigator(),
            new FieldsViewModel(new FakeFieldEditDialogService()));

        var project = new Project("A", DateTime.Now, string.Empty, Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.Single(vm.Rows);

        // Теперь загружаем другую таблицу — 3 строки.
        var secondTable = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[]
            {
                (IReadOnlyList<string>)new[] { "Петров" },
                (IReadOnlyList<string>)new[] { "Сидоров" },
                (IReadOnlyList<string>)new[] { "Кузнецов" }
            });
        var resolver2 = new FakeTableReaderResolver(new FakeTableReader(secondTable));
        var vm2 = new MainViewModel(
            resolver2, storage, new FakeWindowNavigator(),
            new FieldsViewModel(new FakeFieldEditDialogService()));
        vm2.Initialize(project, @"C:\Projects\A");
        vm2.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.Equal(3, vm2.Rows.Count);
        Assert.Equal("Петров", vm2.Rows[0].Cells[0].Value);
        Assert.Equal("Кузнецов", vm2.Rows[2].Cells[0].Value);
    }

    [Fact]
    public void Rows_AreNotDirtyAfterBuild()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = new MainViewModel(
            resolver, storage, new FakeWindowNavigator(),
            new FieldsViewModel(new FakeFieldEditDialogService()));

        var project = new Project("A", DateTime.Now, string.Empty, Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.All(vm.Rows, r => Assert.False(r.IsDirty));
    }

    [Fact]
    public void Rows_EditCell_MakesRowDirty()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = new MainViewModel(
            resolver, storage, new FakeWindowNavigator(),
            new FieldsViewModel(new FakeFieldEditDialogService()));

        var project = new Project("A", DateTime.Now, string.Empty, Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        vm.Rows[0].Cells[0].Value = "Петров";

        Assert.True(vm.Rows[0].IsDirty);
    }
}