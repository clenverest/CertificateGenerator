using CertificateGenerator.Core.Models;
using CertificateGenerator.Tests.Fakes;
using CertificateGenerator.Wpf.ViewModels;
using CertificateGenerator.Core.Validators;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Services;

namespace CertificateGenerator.Tests.ViewModels;

public sealed class MainViewModelTests
{
    private static TableData EmptyTable()
    {
        return new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            Array.Empty<IReadOnlyList<string>>());
    }

    private static MainViewModel CreateViewModel(
        ITableReaderResolver? resolver = null,
        FakeProjectStorage? storage = null,
        FakeWindowNavigator? navigator = null,
        FieldsViewModel? fields = null,
        FakeValidator? validator = null,
        FakeTemplateStorage? templateStorage = null,
        FakePdfGenerator? pdfGenerator = null,
        FakeFilePicker? filePicker = null,
        FakeMessageBoxService? messageBox = null)
    {
        var tmplStorage = templateStorage ?? new FakeTemplateStorage();
        var fp = filePicker ?? new FakeFilePicker();
        var mb = messageBox ?? new FakeMessageBoxService();

        var pdf = pdfGenerator ?? new FakePdfGenerator();
        var zip = new FakeZipPacker();
        var batchGen = new CertificateBatchGenerator(pdf, zip);
        var generation = new GenerationViewModel(batchGen, mb);

        return new MainViewModel(
            resolver ?? new FakeTableReaderResolver(new FakeTableReader(EmptyTable())),
            storage ?? new FakeProjectStorage(),
            navigator ?? new FakeWindowNavigator(),
            fields ?? new FieldsViewModel(new FakeFieldEditDialogService()),
            validator ?? new FakeValidator(),
            new TemplateViewModel(tmplStorage, fp, new FakeTemplateRenderer()),
            tmplStorage,
            pdf,
            fp,
            mb,
            generation);
    }

    private static MainViewModel CreateViewModelWithOverride(
        TableData table,
        out FakeProjectStorage storage)
    {
        storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = CreateViewModel(resolver: resolver, storage: storage);

        var project = new Project(
            "A", DateTime.Now, "data.xlsx", Array.Empty<Field>(),
            rowOverrides: new[]
            {
                new RowOverride(0, new[] { "Сидоров" })
            });
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        return vm;
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
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

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
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

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
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

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
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

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
        var vm = CreateViewModel(storage: storage);

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

        var vm = CreateViewModel(
            storage: storage,
            fields: new FieldsViewModel(dialog));

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
        var vm = CreateViewModel(
            storage: storage,
            fields: new FieldsViewModel(dialog));

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

        var vm = CreateViewModel(
            storage: storage,
            fields: new FieldsViewModel(dialog));

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
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

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
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

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
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

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
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

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
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

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
        var vm2 = CreateViewModel(
            resolver: resolver2,
            storage: storage);
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
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

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
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

        var project = new Project("A", DateTime.Now, string.Empty, Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        vm.Rows[0].Cells[0].Value = "Петров";

        Assert.True(vm.Rows[0].IsDirty);
    }

        // ---------------------------------------------------------------------
    // Валидация
    // ---------------------------------------------------------------------

    [Fact]
    public void Initialize_WithoutTable_ValidationIssuesEmpty()
    {
        var vm = CreateViewModel();
        var project = new Project("A", DateTime.Now, string.Empty, Array.Empty<Field>());

        vm.Initialize(project, @"C:\Projects\A");

        Assert.Empty(vm.ValidationIssues);
    }

    [Fact]
    public void LoadTable_RunsValidation()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var validator = new FakeValidator
        {
            IssuesToReturn = new[]
            {
                new ValidationIssue(IssueSeverity.Error, 0, "ФИО", "test")
            }
        };

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage,
            validator: validator);

        var project = new Project("A", DateTime.Now, string.Empty, Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");

        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.Single(vm.ValidationIssues);
        Assert.Equal("ФИО", vm.ValidationIssues[0].FieldName);
    }

    [Fact]
    public void AddField_RunsValidation()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var validator = new FakeValidator();
        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var dialog = new FakeFieldEditDialogService
        {
            FieldToReturnOnCreate = new Field("ФИО", FieldKind.Simple, new[] { 0 })
        };
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage,
            fields: new FieldsViewModel(dialog),
            validator: validator);

        var project = new Project("A", DateTime.Now, string.Empty, Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");
        vm.Fields.SetColumns(new[] { new ColumnInfo(0, "ФИО") });

        var callsBefore = validator.Calls.Count;
        vm.Fields.AddFieldCommand.Execute(null);

        // Валидация запускалась ещё раз после добавления поля.
        Assert.True(validator.Calls.Count > callsBefore);
    }

    [Fact]
    public void EditCell_RunsValidation()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var validator = new FakeValidator();
        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage,
            validator: validator);

        var project = new Project("A", DateTime.Now, string.Empty, Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        var callsBefore = validator.Calls.Count;
        vm.Rows[0].Cells[0].Value = "Петров";

        Assert.True(validator.Calls.Count > callsBefore);
    }

    [Fact]
    public void EditCellBackToOriginal_StillRunsValidation()
    {
        // Даже если значение вернулось к исходному — валидация пересчитывается.
        // (Оптимизация «не пересчитывать, если ничего не изменилось» — отдельная задача.)
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var validator = new FakeValidator();
        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage,
            validator: validator);

        var project = new Project("A", DateTime.Now, string.Empty, Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        vm.Rows[0].Cells[0].Value = "Петров";   // изменение
        var callsAfterFirstChange = validator.Calls.Count;

        vm.Rows[0].Cells[0].Value = "Иванов";   // назад

        Assert.True(validator.Calls.Count > callsAfterFirstChange);
    }

    // ---------------------------------------------------------------------
    // ValidationRows + ValidationSummary
    // ---------------------------------------------------------------------

    [Fact]
    public void Initialize_WithoutTable_ValidationRowsEmpty()
    {
        var vm = CreateViewModel();
        var project = new Project("A", DateTime.Now, string.Empty, Array.Empty<Field>());

        vm.Initialize(project, @"C:\Projects\A");

        Assert.Empty(vm.ValidationRows);
        Assert.Equal(0, vm.ErrorCount);
        Assert.Equal(0, vm.WarningCount);
        Assert.False(vm.HasIssues);
        Assert.Equal(string.Empty, vm.ValidationSummary);
    }

    [Fact]
    public void LoadTable_NoIssues_ValidationRowsFilled()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[]
            {
                (IReadOnlyList<string>)new[] { "Иванов" },
                (IReadOnlyList<string>)new[] { "Петров" }
            });

        var validator = new FakeValidator();   // нет проблем
        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var dialog = new FakeFieldEditDialogService();
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage,
            fields: new FieldsViewModel(dialog),
            validator: validator);

        // Поле добавляем через LoadFields — иначе диалог вернёт null.
        var fields = new[] { new Field("ФИО", FieldKind.Simple, new[] { 0 }) };
        var project = new Project("A", DateTime.Now, string.Empty, fields);
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.Equal(2, vm.ValidationRows.Count);
        Assert.Single(vm.ValidationRows[0].Cells);
        Assert.Equal("Иванов", vm.ValidationRows[0].Cells[0].Value);
        Assert.False(vm.ValidationRows[0].Cells[0].HasError);
        Assert.Equal("✓ Проблем не найдено", vm.ValidationSummary);
    }

    [Fact]
    public void LoadTable_WithErrors_ValidationRowsHaveErrors()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var validator = new FakeValidator
        {
            IssuesToReturn = new[]
            {
                new ValidationIssue(IssueSeverity.Error, 0, "ФИО", "Обязательно")
            }
        };

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage,
            validator: validator);

        var fields = new[] { new Field("ФИО", FieldKind.Simple, new[] { 0 }) };
        var project = new Project("A", DateTime.Now, string.Empty, fields);
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.Single(vm.ValidationRows);
        Assert.True(vm.ValidationRows[0].Cells[0].HasError);
        Assert.Equal(1, vm.ErrorCount);
        Assert.Equal(0, vm.WarningCount);
        Assert.Equal("Ошибок: 1", vm.ValidationSummary);
    }

    [Fact]
    public void LoadTable_WithWarning_SummaryShowsWarning()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var validator = new FakeValidator
        {
            IssuesToReturn = new[]
            {
                new ValidationIssue(IssueSeverity.Warning, 0, "ФИО", "Дубликат")
            }
        };

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage,
            validator: validator);

        var fields = new[] { new Field("ФИО", FieldKind.Simple, new[] { 0 }) };
        var project = new Project("A", DateTime.Now, string.Empty, fields);
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.True(vm.ValidationRows[0].Cells[0].HasWarning);
        Assert.Equal(0, vm.ErrorCount);
        Assert.Equal(1, vm.WarningCount);
        Assert.Equal("Предупреждений: 1", vm.ValidationSummary);
    }

    [Fact]
    public void LoadTable_WithBothErrorAndWarning_CellHasBoth()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "  Иванов  " } });

        var validator = new FakeValidator
        {
            IssuesToReturn = new[]
            {
                new ValidationIssue(IssueSeverity.Error, 0, "ФИО", "Ошибка"),
                new ValidationIssue(IssueSeverity.Warning, 0, "ФИО", "Пробелы")
            }
        };

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage,
            validator: validator);

        var fields = new[] { new Field("ФИО", FieldKind.Simple, new[] { 0 }) };
        var project = new Project("A", DateTime.Now, string.Empty, fields);
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        var cell = vm.ValidationRows[0].Cells[0];
        Assert.True(cell.HasError);
        Assert.True(cell.HasWarning);
        Assert.Equal("Ошибок: 1 · Предупреждений: 1", vm.ValidationSummary);
    }

    [Fact]
    public void ValidationSummary_ChangesWhenIssuesChange()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var validator = new FakeValidator
        {
            IssuesToReturn = new[]
            {
                new ValidationIssue(IssueSeverity.Error, 0, "ФИО", "test")
            }
        };

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage,
            validator: validator);

        var fields = new[] { new Field("ФИО", FieldKind.Simple, new[] { 0 }) };
        var project = new Project("A", DateTime.Now, string.Empty, fields);
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.Equal("Ошибок: 1", vm.ValidationSummary);

        // Меняем fake — теперь проблем нет.
        validator.IssuesToReturn = Array.Empty<ValidationIssue>();

        // Правка ячейки триггерит RunValidation.
        vm.Rows[0].Cells[0].Value = "Петров";

        Assert.Equal("✓ Проблем не найдено", vm.ValidationSummary);
    }

    [Fact]
    public void ValidationRows_TwoFieldsTwoRows_BuildsCorrectly()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО"), new ColumnInfo(1, "Класс") },
            new[]
            {
                (IReadOnlyList<string>)new[] { "Иванов", "5А" },
                (IReadOnlyList<string>)new[] { "Петров", "6Б" }
            });

        var validator = new FakeValidator();
        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage,
            validator: validator);

        var fields = new[]
        {
            new Field("ФИО", FieldKind.Simple, new[] { 0 }),
            new Field("Класс", FieldKind.Simple, new[] { 1 })
        };
        var project = new Project("A", DateTime.Now, string.Empty, fields);
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.Equal(2, vm.ValidationRows.Count);
        Assert.Equal(2, vm.ValidationRows[0].Cells.Count);
        Assert.Equal("Иванов", vm.ValidationRows[0].Cells[0].Value);
        Assert.Equal("5А", vm.ValidationRows[0].Cells[1].Value);
        Assert.Equal("Петров", vm.ValidationRows[1].Cells[0].Value);
        Assert.Equal("6Б", vm.ValidationRows[1].Cells[1].Value);
    }

    [Fact]
    public void Initialize_ProjectWithOverrides_AppliesThemToRows()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[]
            {
                (IReadOnlyList<string>)new[] { "Иванов" },
                (IReadOnlyList<string>)new[] { "Петров" }
            });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

        var project = new Project(
            "A", DateTime.Now, "data.xlsx",
            Array.Empty<Field>(),
            rowOverrides: new[]
            {
                new RowOverride(0, new[] { "Сидоров" })
            });

        vm.Initialize(project, @"C:\Projects\A");

        Assert.Equal("Сидоров", vm.Rows[0].Cells[0].Value);
        Assert.Equal("Петров", vm.Rows[1].Cells[0].Value);
    }

        [Fact]
    public void LoadTableIntoProjectFromPath_AppliesOverridesFromProject()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

        var project = new Project(
            "A", DateTime.Now, string.Empty,
            Array.Empty<Field>(),
            rowOverrides: new[]
            {
                new RowOverride(0, new[] { "Сидоров" })
            });
        vm.Initialize(project, @"C:\Projects\A");

        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.Single(vm.Rows);
        Assert.Equal("Сидоров", vm.Rows[0].Cells[0].Value);
    }

    // ---------------------------------------------------------------------
    // SaveRowOverride
    // ---------------------------------------------------------------------

    [Fact]
    public void EditCell_SavesOverrideToProject()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

        var project = new Project("A", DateTime.Now, "data.xlsx", Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        vm.Rows[0].Cells[0].Value = "Петров";

        // Override сохранён в CurrentProject.
        Assert.Single(vm.CurrentProject!.RowOverrides);
        Assert.Equal(0, vm.CurrentProject.RowOverrides[0].RowIndex);
        Assert.Equal("Петров", vm.CurrentProject.RowOverrides[0].Values[0]);
    }

    [Fact]
    public void EditCell_SavesToStorage()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

        var project = new Project("A", DateTime.Now, "data.xlsx", Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        var savesBefore = storage.SaveCalls.Count;
        vm.Rows[0].Cells[0].Value = "Петров";

        // Было как минимум два Save (при инициализации и загрузке таблицы),
        // стало — минимум на один больше.
        Assert.True(storage.SaveCalls.Count > savesBefore);
    }

    [Fact]
    public void EditCellBackToOriginal_RemovesOverride()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

        var project = new Project("A", DateTime.Now, "data.xlsx", Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        vm.Rows[0].Cells[0].Value = "Петров";   // override появился
        Assert.Single(vm.CurrentProject!.RowOverrides);

        vm.Rows[0].Cells[0].Value = "Иванов";   // вернули исходное

        Assert.Empty(vm.CurrentProject!.RowOverrides);
    }

    [Fact]
    public void EditSecondRow_AddsTwoOverrides()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[]
            {
                (IReadOnlyList<string>)new[] { "Иванов" },
                (IReadOnlyList<string>)new[] { "Петров" }
            });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

        var project = new Project("A", DateTime.Now, "data.xlsx", Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        vm.Rows[0].Cells[0].Value = "Сидоров";
        vm.Rows[1].Cells[0].Value = "Кузнецов";

        Assert.Equal(2, vm.CurrentProject!.RowOverrides.Count);
    }

    [Fact]
    public void EditCellTwice_ReplacesOverrideNotDuplicates()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

        var project = new Project("A", DateTime.Now, "data.xlsx", Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        vm.Rows[0].Cells[0].Value = "Петров";
        vm.Rows[0].Cells[0].Value = "Сидоров";

        Assert.Single(vm.CurrentProject!.RowOverrides);
        Assert.Equal("Сидоров", vm.CurrentProject.RowOverrides[0].Values[0]);
    }

    [Fact]
    public void ResetChanges_WithoutOverrides_CannotExecute()
    {
        var vm = CreateViewModel();

        Assert.False(vm.ResetChangesCommand.CanExecute(null));
    }

    [Fact]
    public void ResetChanges_WithOverrides_CanExecute()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var vm = CreateViewModelWithOverride(table, out _);

        Assert.True(vm.ResetChangesCommand.CanExecute(null));
    }

    [Fact]
    public void EditCell_ThenResetChangesCommandBecomesAvailable()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = CreateViewModel(
            resolver: resolver,
            storage: storage);

        var project = new Project("A", DateTime.Now, "data.xlsx", Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.False(vm.ResetChangesCommand.CanExecute(null));

        vm.Rows[0].Cells[0].Value = "Петров";

        Assert.True(vm.ResetChangesCommand.CanExecute(null));
    }
}