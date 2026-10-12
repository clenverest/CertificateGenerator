using CertificateGenerator.Core.Models;
using CertificateGenerator.Infrastructure.Storage;

namespace CertificateGenerator.Tests.Storage;

public sealed class JsonRecentProjectsStoreTests
{
    private static JsonRecentProjectsStore CreateStore(TempFolder temp)
    {
        var filePath = System.IO.Path.Combine(temp.Path, "recent.json");
        return new JsonRecentProjectsStore(filePath);
    }

    [Fact]
    public void GetAll_FileDoesNotExist_ReturnsEmpty()
    {
        using var temp = new TempFolder();
        var store = CreateStore(temp);

        var entries = store.GetAll();

        Assert.Empty(entries);
    }

    [Fact]
    public void Add_OneEntry_GetAllReturnsIt()
    {
        using var temp = new TempFolder();
        var store = CreateStore(temp);

        store.Add(@"C:\Projects\Осень 2026", "Осень 2026");

        var entries = store.GetAll();
        Assert.Single(entries);
        Assert.Equal(@"C:\Projects\Осень 2026", entries[0].ProjectFolderPath);
        Assert.Equal("Осень 2026", entries[0].ProjectName);
    }

    [Fact]
    public void Add_MultipleEntries_MostRecentFirst()
    {
        using var temp = new TempFolder();
        var store = CreateStore(temp);

        store.Add(@"C:\Projects\A", "Проект A");
        store.Add(@"C:\Projects\B", "Проект B");
        store.Add(@"C:\Projects\C", "Проект C");

        var entries = store.GetAll();
        Assert.Equal(3, entries.Count);
        Assert.Equal("Проект C", entries[0].ProjectName);
        Assert.Equal("Проект B", entries[1].ProjectName);
        Assert.Equal("Проект A", entries[2].ProjectName);
    }

    [Fact]
    public void Add_ExistingPath_MovesToTopAndUpdatesName()
    {
        using var temp = new TempFolder();
        var store = CreateStore(temp);

        store.Add(@"C:\Projects\A", "Старое имя");
        store.Add(@"C:\Projects\B", "Проект B");
        store.Add(@"C:\Projects\A", "Новое имя");

        var entries = store.GetAll();
        Assert.Equal(2, entries.Count);
        Assert.Equal("Новое имя", entries[0].ProjectName);
        Assert.Equal(@"C:\Projects\A", entries[0].ProjectFolderPath);
        Assert.Equal("Проект B", entries[1].ProjectName);
    }

    [Fact]
    public void Add_MoreThanMaxEntries_TrimsOldest()
    {
        using var temp = new TempFolder();
        var store = CreateStore(temp);

        for (int i = 0; i < 15; i++)
        {
            store.Add($@"C:\Projects\P{i}", $"Проект {i}");
        }

        var entries = store.GetAll();
        Assert.Equal(10, entries.Count);
        // Сверху — самые свежие (P14), снизу — самые старые из оставшихся (P5).
        Assert.Equal("Проект 14", entries[0].ProjectName);
        Assert.Equal("Проект 5", entries[9].ProjectName);
    }

    [Fact]
    public void Remove_ExistingEntry_RemovesIt()
    {
        using var temp = new TempFolder();
        var store = CreateStore(temp);
        store.Add(@"C:\Projects\A", "Проект A");
        store.Add(@"C:\Projects\B", "Проект B");

        store.Remove(@"C:\Projects\A");

        var entries = store.GetAll();
        Assert.Single(entries);
        Assert.Equal("Проект B", entries[0].ProjectName);
    }

    [Fact]
    public void Remove_NonExistingEntry_DoesNothing()
    {
        using var temp = new TempFolder();
        var store = CreateStore(temp);
        store.Add(@"C:\Projects\A", "Проект A");

        store.Remove(@"C:\Projects\НетТакого");

        var entries = store.GetAll();
        Assert.Single(entries);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        using var temp = new TempFolder();
        var store = CreateStore(temp);
        store.Add(@"C:\Projects\A", "Проект A");
        store.Add(@"C:\Projects\B", "Проект B");

        store.Clear();

        Assert.Empty(store.GetAll());
    }

    [Fact]
    public void GetAll_CorruptedFile_ReturnsEmpty()
    {
        using var temp = new TempFolder();
        var filePath = System.IO.Path.Combine(temp.Path, "recent.json");
        File.WriteAllText(filePath, "{ это не json }");
        var store = new JsonRecentProjectsStore(filePath);

        var entries = store.GetAll();

        Assert.Empty(entries);
    }

    [Fact]
    public void Persistence_AddThenNewStore_ReadsSameData()
    {
        using var temp = new TempFolder();
        var filePath = System.IO.Path.Combine(temp.Path, "recent.json");

        var store1 = new JsonRecentProjectsStore(filePath);
        store1.Add(@"C:\Projects\A", "Проект A");
        store1.Add(@"C:\Projects\B", "Проект B");

        var store2 = new JsonRecentProjectsStore(filePath);
        var entries = store2.GetAll();

        Assert.Equal(2, entries.Count);
        Assert.Equal("Проект B", entries[0].ProjectName);
        Assert.Equal("Проект A", entries[1].ProjectName);
    }
}