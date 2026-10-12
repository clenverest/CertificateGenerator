using CertificateGenerator.Core.Models;
using CertificateGenerator.Infrastructure.Storage;

namespace CertificateGenerator.Tests.Storage;

public sealed class JsonProjectStorageFileTests
{
    private readonly JsonProjectStorage _storage = new();

    // -----------------------------------------------------------------
    // Create
    // -----------------------------------------------------------------

    [Fact]
    public void Create_InNewFolder_CreatesProjectStructureAndFile()
    {
        using var temp = new TempFolder();
        var projectFolder = System.IO.Path.Combine(temp.Path, "MyProject");

        var project = _storage.Create(projectFolder, "Мой проект");

        Assert.Equal("Мой проект", project.Name);
        Assert.Equal(string.Empty, project.SourceFileName);
        Assert.Empty(project.Fields);

        Assert.True(Directory.Exists(projectFolder));
        Assert.True(Directory.Exists(System.IO.Path.Combine(projectFolder, "source")));
        Assert.True(Directory.Exists(System.IO.Path.Combine(projectFolder, "template")));
        Assert.True(Directory.Exists(System.IO.Path.Combine(projectFolder, "output")));
        Assert.True(File.Exists(System.IO.Path.Combine(projectFolder, "project.json")));
    }

    [Fact]
    public void Create_InNonEmptyFolder_ThrowsInvalidOperationException()
    {
        using var temp = new TempFolder();
        // temp.Path уже не пустой: TempFolder создал папку,
        // но она пуста. Кладём туда файл — теперь не пуста.
        File.WriteAllText(System.IO.Path.Combine(temp.Path, "чужой.txt"), "не трогать");

        Assert.Throws<InvalidOperationException>(() =>
            _storage.Create(temp.Path, "Проект"));
    }

    [Fact]
    public void Create_EmptyFolderPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _storage.Create(string.Empty, "Проект"));
    }

    [Fact]
    public void Create_EmptyProjectName_ThrowsArgumentException()
    {
        using var temp = new TempFolder();
        var projectFolder = System.IO.Path.Combine(temp.Path, "MyProject");

        Assert.Throws<ArgumentException>(() =>
            _storage.Create(projectFolder, string.Empty));
    }

    // -----------------------------------------------------------------
    // Load
    // -----------------------------------------------------------------

    [Fact]
    public void Load_AfterCreate_ReturnsSameProject()
    {
        using var temp = new TempFolder();
        var projectFolder = System.IO.Path.Combine(temp.Path, "MyProject");
        var created = _storage.Create(projectFolder, "Мой проект");

        var loaded = _storage.Load(projectFolder);

        Assert.Equal(created.Name, loaded.Name);
        Assert.Equal(created.SourceFileName, loaded.SourceFileName);
        Assert.Equal(created.Fields.Count, loaded.Fields.Count);
        // CreatedAt может отличаться на доли секунды — не сравниваем точно.
        // Проверяем, что значение «разумное»: близко к created.
        Assert.True((loaded.CreatedAt - created.CreatedAt).Duration() < TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Load_FolderWithoutProjectFile_ThrowsFileNotFoundException()
    {
        using var temp = new TempFolder();

        Assert.Throws<FileNotFoundException>(() =>
            _storage.Load(temp.Path));
    }

    // -----------------------------------------------------------------
    // Save + Load round-trip с изменённым проектом
    // -----------------------------------------------------------------

    [Fact]
    public void Save_ModifiedProject_ThenLoad_PreservesChanges()
    {
        using var temp = new TempFolder();
        var projectFolder = System.IO.Path.Combine(temp.Path, "MyProject");
        _storage.Create(projectFolder, "Исходный");

        var modified = new Project(
            "Изменённый",
            new DateTime(2026, 10, 12, 14, 30, 0),
            "data.xlsx",
            new[]
            {
                new Field("ФИО", FieldKind.FullName, new[] { 0, 1, 2 }, isRequired: true),
                new Field("Класс", FieldKind.Simple, new[] { 3 })
            });

        _storage.Save(modified, projectFolder);
        var loaded = _storage.Load(projectFolder);

        Assert.Equal("Изменённый", loaded.Name);
        Assert.Equal(new DateTime(2026, 10, 12, 14, 30, 0), loaded.CreatedAt);
        Assert.Equal("data.xlsx", loaded.SourceFileName);
        Assert.Equal(2, loaded.Fields.Count);
        Assert.Equal("ФИО", loaded.Fields[0].Name);
        Assert.Equal(FieldKind.FullName, loaded.Fields[0].Kind);
        Assert.True(loaded.Fields[0].IsRequired);
        Assert.Equal("Класс", loaded.Fields[1].Name);
    }

    [Fact]
    public void Save_NonExistingFolder_ThrowsDirectoryNotFoundException()
    {
        using var temp = new TempFolder();
        var nonExisting = System.IO.Path.Combine(temp.Path, "НетТакой");

        var project = new Project(
            "Проект",
            DateTime.Now,
            string.Empty,
            Array.Empty<Field>());

        Assert.Throws<DirectoryNotFoundException>(() =>
            _storage.Save(project, nonExisting));
    }

    // -----------------------------------------------------------------
    // Exists
    // -----------------------------------------------------------------

    [Fact]
    public void Exists_ProjectFolder_ReturnsTrue()
    {
        using var temp = new TempFolder();
        var projectFolder = System.IO.Path.Combine(temp.Path, "MyProject");
        _storage.Create(projectFolder, "Проект");

        Assert.True(_storage.Exists(projectFolder));
    }

    [Fact]
    public void Exists_EmptyFolder_ReturnsFalse()
    {
        using var temp = new TempFolder();

        Assert.False(_storage.Exists(temp.Path));
    }

    [Fact]
    public void Exists_NonExistingFolder_ReturnsFalse()
    {
        using var temp = new TempFolder();
        var nonExisting = System.IO.Path.Combine(temp.Path, "НетТакой");

        Assert.False(_storage.Exists(nonExisting));
    }

    [Fact]
    public void Exists_EmptyPath_ReturnsFalse()
    {
        Assert.False(_storage.Exists(string.Empty));
    }
}