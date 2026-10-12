using System.IO;
using CertificateGenerator.Core.Models;
using CertificateGenerator.Tests.Fakes;
using CertificateGenerator.Wpf.ViewModels;
using Xunit;

namespace CertificateGenerator.Tests.ViewModels;

public class MainViewModelTests
{
    private static TableData CreateSampleTable()
    {
        var columns = new List<ColumnInfo>
        {
            new ColumnInfo(0, "ФИО"),
            new ColumnInfo(1, "Класс"),
        };
        var rows = new List<IReadOnlyList<string>>
        {
            new List<string> { "Иванов Иван", "5А" },
        };
        return new TableData(columns, rows);
    }

    [Fact]
    public void Constructor_Throws_On_Null_Resolver()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MainViewModel(null!));
    }

    [Fact]
    public void LoadTable_Success_Sets_TableData()
    {
        var table = CreateSampleTable();
        var reader = new FakeTableReader(table);
        var resolver = new FakeTableReaderResolver(reader);
        var vm = new MainViewModel(resolver);

        vm.LoadTable("any.xlsx");

        Assert.Same(table, vm.TableData);
        Assert.Null(vm.ErrorMessage);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public void LoadTable_Clears_Previous_Error()
    {
        var table = CreateSampleTable();
        var reader = new FakeTableReader(table);
        var resolver = new FakeTableReaderResolver(reader);
        var vm = new MainViewModel(resolver);

        vm.LoadTable("any.xlsx");
        Assert.Null(vm.ErrorMessage);

        vm.LoadTable("any.xlsx");
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public void LoadTable_InvalidData_Sets_ErrorMessage()
    {
        var resolver = new ThrowingTableReaderResolver(
            new InvalidDataException("битый файл"));
        var vm = new MainViewModel(resolver);

        vm.LoadTable("any.xlsx");

        Assert.Null(vm.TableData);
        Assert.NotNull(vm.ErrorMessage);
        Assert.Contains("битый файл", vm.ErrorMessage);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public void LoadTable_FileNotFound_Sets_ErrorMessage()
    {
        var resolver = new ThrowingTableReaderResolver(
            new FileNotFoundException("нет файла"));
        var vm = new MainViewModel(resolver);

        vm.LoadTable("any.xlsx");

        Assert.Null(vm.TableData);
        Assert.NotNull(vm.ErrorMessage);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public void LoadTable_IOException_Sets_ErrorMessage()
    {
        var resolver = new ThrowingTableReaderResolver(
            new IOException("файл занят"));
        var vm = new MainViewModel(resolver);

        vm.LoadTable("any.xlsx");

        Assert.Null(vm.TableData);
        Assert.NotNull(vm.ErrorMessage);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public void LoadTable_Resets_TableData_On_Error()
    {
        var table = CreateSampleTable();
        var reader = new FakeTableReader(table);
        var resolver = new FakeTableReaderResolver(reader);
        var vm = new MainViewModel(resolver);

        vm.LoadTable("any.xlsx");
        Assert.NotNull(vm.TableData);

        var throwingResolver = new ThrowingTableReaderResolver(
            new InvalidDataException("битый файл"));
        var vm2 = new MainViewModel(throwingResolver);
        vm2.LoadTable("any.xlsx");

        Assert.Null(vm2.TableData);
    }

    [Fact]
    public void LoadTable_Resets_IsBusy_On_Error()
    {
        var resolver = new ThrowingTableReaderResolver(
            new InvalidDataException("битый файл"));
        var vm = new MainViewModel(resolver);

        vm.LoadTable("any.xlsx");

        Assert.False(vm.IsBusy);
    }

    [Fact]
    public void HasError_Is_False_By_Default()
    {
        var resolver = new FakeTableReaderResolver(new FakeTableReader(CreateSampleTable()));
        var vm = new MainViewModel(resolver);

        Assert.False(vm.HasError);
    }

    [Fact]
    public void HasError_Is_True_After_Error()
    {
        var resolver = new ThrowingTableReaderResolver(new InvalidDataException("битый"));
        var vm = new MainViewModel(resolver);

        vm.LoadTable("any.xlsx");

        Assert.True(vm.HasError);
    }

    [Fact]
    public void HasError_Is_False_After_Success()
    {
        var resolver = new FakeTableReaderResolver(new FakeTableReader(CreateSampleTable()));
        var vm = new MainViewModel(resolver);

        vm.LoadTable("any.xlsx");

        Assert.False(vm.HasError);
    }

    [Fact]
    public void TableSummary_Is_Null_When_No_Table()
    {
        var resolver = new FakeTableReaderResolver(new FakeTableReader(CreateSampleTable()));
        var vm = new MainViewModel(resolver);

        Assert.Null(vm.TableSummary);
    }

    [Fact]
    public void TableSummary_Is_Not_Null_After_Success()
    {
        var resolver = new FakeTableReaderResolver(new FakeTableReader(CreateSampleTable()));
        var vm = new MainViewModel(resolver);

        vm.LoadTable("any.xlsx");

        Assert.NotNull(vm.TableSummary);
        Assert.Contains("Загружено", vm.TableSummary);
    }

    [Fact]
    public void TableSummary_Contains_Row_And_Column_Counts()
    {
        var table = CreateSampleTable();
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var vm = new MainViewModel(resolver);

        vm.LoadTable("any.xlsx");

        Assert.Contains(table.RowCount.ToString(), vm.TableSummary);
        Assert.Contains(table.ColumnCount.ToString(), vm.TableSummary);
    }

    [Fact]
    public void HasTableSummary_Is_False_When_No_Table()
    {
        var resolver = new FakeTableReaderResolver(new FakeTableReader(CreateSampleTable()));
        var vm = new MainViewModel(resolver);

        Assert.False(vm.HasTableSummary);
    }

    [Fact]
    public void HasTableSummary_Is_True_After_Success()
    {
        var resolver = new FakeTableReaderResolver(new FakeTableReader(CreateSampleTable()));
        var vm = new MainViewModel(resolver);

        vm.LoadTable("any.xlsx");

        Assert.True(vm.HasTableSummary);
    }
}