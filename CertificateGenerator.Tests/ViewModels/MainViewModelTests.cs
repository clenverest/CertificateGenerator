using CertificateGenerator.Core.Models;
using CertificateGenerator.Tests.Fakes;
using CertificateGenerator.Wpf.ViewModels;

namespace CertificateGenerator.Tests.ViewModels;

public sealed class MainViewModelTests
{

    public string? GetSourceTablePathToReturn { get; set; }
    public Exception? ExceptionToThrowOnSetSourceTable { get; set; }

    private static MainViewModel CreateViewModel()
    {
        var emptyTable = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            Array.Empty<IReadOnlyList<string>>());

        return new MainViewModel(
            new FakeTableReaderResolver(new FakeTableReader(emptyTable)),
            new FakeProjectStorage(),
            new FakeWindowNavigator());
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
        var vm = new MainViewModel(resolver, storage, new FakeWindowNavigator());

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
        var resolver = new FakeTableReaderResolver(new FakeTableReader(
            new TableData(
                new[] { new ColumnInfo(0, "ФИО") },
                Array.Empty<IReadOnlyList<string>>())));
        var vm = new MainViewModel(resolver, storage, new FakeWindowNavigator());

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
        var resolver = new FakeTableReaderResolver(new FakeTableReader(
            new TableData(
                new[] { new ColumnInfo(0, "ФИО") },
                Array.Empty<IReadOnlyList<string>>())));
        var vm = new MainViewModel(resolver, storage, new FakeWindowNavigator());

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
        var vm = new MainViewModel(resolver, storage, new FakeWindowNavigator());

        var project = new Project("A", DateTime.Now, "data.xlsx", Array.Empty<Field>());
        vm.Initialize(project, @"C:\Projects\A");

        Assert.NotNull(vm.TableData);
        Assert.Single(vm.TableData!.Rows);
        Assert.Equal(@"C:\Projects\A\source\data.xlsx", vm.SelectedFilePath);
    }
}