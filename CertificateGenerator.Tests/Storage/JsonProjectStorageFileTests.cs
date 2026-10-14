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

    // -----------------------------------------------------------------
// SetSourceTable
// -----------------------------------------------------------------

    [Fact]
    public void SetSourceTable_CopiesXlsxFileToSourceFolder()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Проект");

        var sourceFile = Path.Combine(temp.Path, "Участники.xlsx");
        File.WriteAllText(sourceFile, "fake xlsx");

        var updated = _storage.SetSourceTable(projectFolder, project, sourceFile);

        Assert.Equal("Участники.xlsx", updated.SourceFileName);
        var expectedPath = Path.Combine(projectFolder, "source", "Участники.xlsx");
        Assert.True(File.Exists(expectedPath));
    }

    [Fact]
    public void SetSourceTable_CopiesCsvFile()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Проект");

        var sourceFile = Path.Combine(temp.Path, "Участники.csv");
        File.WriteAllText(sourceFile, "fake csv");

        var updated = _storage.SetSourceTable(projectFolder, project, sourceFile);

        Assert.Equal("Участники.csv", updated.SourceFileName);
        var expectedPath = Path.Combine(projectFolder, "source", "Участники.csv");
        Assert.True(File.Exists(expectedPath));
    }

    [Fact]
    public void SetSourceTable_ReplacesOldTable()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Проект");

        var firstFile = Path.Combine(temp.Path, "Первый.xlsx");
        File.WriteAllText(firstFile, "first");
        var afterFirst = _storage.SetSourceTable(projectFolder, project, firstFile);

        var secondFile = Path.Combine(temp.Path, "Второй.csv");
        File.WriteAllText(secondFile, "second");
        var afterSecond = _storage.SetSourceTable(projectFolder, afterFirst, secondFile);

        Assert.Equal("Второй.csv", afterSecond.SourceFileName);

        var sourceFolder = Path.Combine(projectFolder, "source");
        var files = Directory.GetFiles(sourceFolder);
        Assert.Single(files);
        Assert.EndsWith("Второй.csv", files[0]);
    }

    [Fact]
    public void SetSourceTable_UpdatesProjectJson()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Проект");

        var sourceFile = Path.Combine(temp.Path, "Новый.xlsx");
        File.WriteAllText(sourceFile, "data");

        _storage.SetSourceTable(projectFolder, project, sourceFile);
        var reloaded = _storage.Load(projectFolder);

        Assert.Equal("Новый.xlsx", reloaded.SourceFileName);
    }

    [Fact]
    public void SetSourceTable_SourceFileMissing_ThrowsFileNotFoundException()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Проект");

        var missingFile = Path.Combine(temp.Path, "НетТакого.xlsx");

        Assert.Throws<FileNotFoundException>(() =>
            _storage.SetSourceTable(projectFolder, project, missingFile));
    }

    [Fact]
    public void SetSourceTable_ProjectFolderMissing_ThrowsDirectoryNotFoundException()
    {
        using var temp = new TempFolder();
        var nonExistentFolder = Path.Combine(temp.Path, "НетТакой");

        var sourceFile = Path.Combine(temp.Path, "Файл.xlsx");
        File.WriteAllText(sourceFile, "data");

        var project = new Project(
            "Проект", DateTime.Now, string.Empty, Array.Empty<Field>());

        Assert.Throws<DirectoryNotFoundException>(() =>
            _storage.SetSourceTable(nonExistentFolder, project, sourceFile));
    }

// -----------------------------------------------------------------
// GetSourceTablePath
// -----------------------------------------------------------------

    [Fact]
    public void GetSourceTablePath_AfterSetSourceTable_ReturnsPath()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Проект");

        var sourceFile = Path.Combine(temp.Path, "Участники.xlsx");
        File.WriteAllText(sourceFile, "data");
        var updated = _storage.SetSourceTable(projectFolder, project, sourceFile);

        var path = _storage.GetSourceTablePath(updated, projectFolder);

        Assert.NotNull(path);
        Assert.True(File.Exists(path));
        Assert.EndsWith("Участники.xlsx", path);
    }

    [Fact]
    public void GetSourceTablePath_NoSourceTable_ReturnsNull()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Проект");

        var path = _storage.GetSourceTablePath(project, projectFolder);

        Assert.Null(path);
    }

    [Fact]
    public void GetSourceTablePath_SourceFileNameSetButFileMissing_ReturnsNull()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Проект");

        var projectWithMissingFile = new Project(
            project.Name,
            project.CreatedAt,
            "Пропавший.xlsx",
            project.Fields);

        var path = _storage.GetSourceTablePath(projectWithMissingFile, projectFolder);

        Assert.Null(path);
    }

    // -----------------------------------------------------------------
    // RowOverrides — сериализация через Serialize / Deserialize
    // -----------------------------------------------------------------

    [Fact]
    public void Serialize_ProjectWithoutRowOverrides_ContainsEmptyArray()
    {
        var project = new Project(
            "Проект", DateTime.Now, "data.xlsx", Array.Empty<Field>());

        var json = JsonProjectStorage.Serialize(project);

        Assert.Contains("rowOverrides", json);
        Assert.Contains("[]", json);
    }

    [Fact]
    public void Serialize_ProjectWithRowOverrides_ContainsValues()
    {
        var overrides = new[]
        {
            new RowOverride(0, new[] { "Иванов", "Иван" })
        };
        var project = new Project(
            "Проект", DateTime.Now, "data.xlsx",
            Array.Empty<Field>(),
            rowOverrides: overrides);

        var json = JsonProjectStorage.Serialize(project);

        Assert.Contains("rowOverrides", json);
        Assert.Contains("Иванов", json);
        Assert.Contains("Иван", json);
    }

    [Fact]
    public void Deserialize_ProjectWithRowOverrides_RestoresThem()
    {
        var json = """
        {
          "name": "Проект",
          "createdAt": "2026-01-01T00:00:00",
          "sourceFileName": "data.xlsx",
          "fields": [],
          "rowOverrides": [
            {
              "rowIndex": 0,
              "values": ["Иванов", "Иван"]
            },
            {
              "rowIndex": 2,
              "values": ["Петров", "Пётр"]
            }
          ]
        }
        """;

        var project = JsonProjectStorage.Deserialize(json);

        Assert.Equal(2, project.RowOverrides.Count);
        Assert.Equal(0, project.RowOverrides[0].RowIndex);
        Assert.Equal("Иванов", project.RowOverrides[0].Values[0]);
        Assert.Equal(2, project.RowOverrides[1].RowIndex);
    }

    [Fact]
    public void Deserialize_OldJsonWithoutRowOverrides_ReturnsEmptyList()
    {
        // Старый project.json (до 2.10) — без поля rowOverrides.
        var json = """
        {
          "name": "Старый проект",
          "createdAt": "2026-01-01T00:00:00",
          "sourceFileName": "data.xlsx",
          "fields": []
        }
        """;

        var project = JsonProjectStorage.Deserialize(json);

        Assert.NotNull(project.RowOverrides);
        Assert.Empty(project.RowOverrides);
    }

    // -----------------------------------------------------------------
    // RowOverrides — round-trip через файл
    // -----------------------------------------------------------------

    [Fact]
    public void Save_ProjectWithRowOverrides_ThenLoad_PreservesThem()
    {
        using var temp = new TempFolder();
        var projectFolder = System.IO.Path.Combine(temp.Path, "MyProject");
        _storage.Create(projectFolder, "Проект");

        var overrides = new[]
        {
            new RowOverride(0, new[] { "Иванов", "Иван", "Иванович" }),
            new RowOverride(3, new[] { "", "Пётр", "" })
        };
        var modified = new Project(
            "Изменённый",
            new DateTime(2026, 10, 13, 12, 0, 0),
            "data.xlsx",
            Array.Empty<Field>(),
            rowOverrides: overrides);

        _storage.Save(modified, projectFolder);
        var loaded = _storage.Load(projectFolder);

        Assert.Equal(2, loaded.RowOverrides.Count);
        Assert.Equal(0, loaded.RowOverrides[0].RowIndex);
        Assert.Equal("Иванов", loaded.RowOverrides[0].Values[0]);
        Assert.Equal(3, loaded.RowOverrides[1].RowIndex);
        Assert.Equal("", loaded.RowOverrides[1].Values[0]);
        Assert.Equal("Пётр", loaded.RowOverrides[1].Values[1]);
    }

    [Fact]
    public void Save_ProjectWithoutRowOverrides_ThenLoad_ReturnsEmptyList()
    {
        using var temp = new TempFolder();
        var projectFolder = System.IO.Path.Combine(temp.Path, "MyProject");
        _storage.Create(projectFolder, "Проект");

        var modified = new Project(
            "Изменённый",
            new DateTime(2026, 10, 13, 12, 0, 0),
            "data.xlsx",
            Array.Empty<Field>());

        _storage.Save(modified, projectFolder);
        var loaded = _storage.Load(projectFolder);

        Assert.NotNull(loaded.RowOverrides);
        Assert.Empty(loaded.RowOverrides);
    }

    // -----------------------------------------------------------------
    // SetTemplate
    // -----------------------------------------------------------------

    [Fact]
    public void SetTemplate_StoresTemplate()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Проект");

        var template = new TemplateInfo("template.png", 2480, 3508);
        var updated = _storage.SetTemplate(projectFolder, project, template);

        Assert.NotNull(updated.Template);
        Assert.Equal("template.png", updated.Template!.FileName);
        Assert.Equal(2480, updated.Template.Width);
        Assert.Equal(3508, updated.Template.Height);
    }

    [Fact]
    public void SetTemplate_SavesToDisk()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Проект");

        var template = new TemplateInfo("template.png", 100, 200);
        _storage.SetTemplate(projectFolder, project, template);

        var loaded = _storage.Load(projectFolder);

        Assert.NotNull(loaded.Template);
        Assert.Equal("template.png", loaded.Template!.FileName);
        Assert.Equal(100, loaded.Template.Width);
        Assert.Equal(200, loaded.Template.Height);
    }

    [Fact]
    public void SetTemplate_DoesNotTouchOtherFields()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Исходное имя");

        var withFields = new Project(
            project.Name,
            project.CreatedAt,
            project.SourceFileName,
            new[]
            {
                new Field("ФИО", FieldKind.FullName, new[] { 0, 1 })
            },
            new[]
            {
                new RowOverride(0, new[] { "Иванов", "Иван" })
            });

        _storage.Save(withFields, projectFolder);

        var template = new TemplateInfo("template.png", 10, 20);
        var updated = _storage.SetTemplate(projectFolder, withFields, template);

        Assert.Equal("Исходное имя", updated.Name);
        Assert.Single(updated.Fields);
        Assert.Single(updated.RowOverrides);
        Assert.NotNull(updated.Template);
    }

    [Fact]
    public void SetTemplate_PreservesPlacements()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Проект");

        var placements = new[]
        {
            new FieldPlacement("ФИО", 100, 200, 48, "Arial", FieldAlignment.Center)
        };

        var withPlacements = _storage.SetPlacements(projectFolder, project, placements);

        var template = new TemplateInfo("template.png", 10, 20);
        var updated = _storage.SetTemplate(projectFolder, withPlacements, template);

        Assert.Single(updated.Placements);
        Assert.Equal("ФИО", updated.Placements[0].FieldName);
    }

    [Fact]
    public void SetTemplate_NullProject_ThrowsArgumentNullException()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");

        Assert.Throws<ArgumentNullException>(() =>
            _storage.SetTemplate(
                projectFolder, null!, new TemplateInfo("t.png", 10, 10)));
    }

    [Fact]
    public void SetTemplate_NullTemplate_ThrowsArgumentNullException()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Проект");

        Assert.Throws<ArgumentNullException>(() =>
            _storage.SetTemplate(projectFolder, project, null!));
    }

    [Fact]
    public void SetTemplate_ProjectFolderMissing_ThrowsDirectoryNotFoundException()
    {
        using var temp = new TempFolder();
        var nonExisting = Path.Combine(temp.Path, "НетТакой");

        var project = new Project(
            "Проект", DateTime.Now, string.Empty, Array.Empty<Field>());

        Assert.Throws<DirectoryNotFoundException>(() =>
            _storage.SetTemplate(
                nonExisting, project, new TemplateInfo("t.png", 10, 10)));
    }

    // -----------------------------------------------------------------
    // SetPlacements
    // -----------------------------------------------------------------

    [Fact]
    public void SetPlacements_StoresPlacements()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Проект");

        var placements = new[]
        {
            new FieldPlacement("ФИО", 100, 200, 48, "Arial", FieldAlignment.Center),
            new FieldPlacement("Класс", 100, 300, 32, "Arial", FieldAlignment.Left)
        };

        var updated = _storage.SetPlacements(projectFolder, project, placements);

        Assert.Equal(2, updated.Placements.Count);
        Assert.Equal("ФИО", updated.Placements[0].FieldName);
        Assert.Equal("Класс", updated.Placements[1].FieldName);
    }

    [Fact]
    public void SetPlacements_SavesToDisk()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Проект");

        var placements = new[]
        {
            new FieldPlacement("ФИО", 100, 200, 48, "Arial", FieldAlignment.Center)
        };

        _storage.SetPlacements(projectFolder, project, placements);
        var loaded = _storage.Load(projectFolder);

        Assert.Single(loaded.Placements);
        Assert.Equal("ФИО", loaded.Placements[0].FieldName);
        Assert.Equal(100, loaded.Placements[0].X);
        Assert.Equal(200, loaded.Placements[0].Y);
    }

    [Fact]
    public void SetPlacements_EmptyList_ClearsPlacements()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Проект");

        var placements = new[]
        {
            new FieldPlacement("ФИО", 100, 200, 48, "Arial", FieldAlignment.Center)
        };

        var withPlacements = _storage.SetPlacements(projectFolder, project, placements);
        var cleared = _storage.SetPlacements(
            projectFolder, withPlacements, Array.Empty<FieldPlacement>());

        Assert.Empty(cleared.Placements);
    }

    [Fact]
    public void SetPlacements_PreservesTemplate()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Проект");

        var template = new TemplateInfo("t.png", 100, 200);
        var withTemplate = _storage.SetTemplate(projectFolder, project, template);

        var placements = new[]
        {
            new FieldPlacement("ФИО", 10, 20, 30, "Arial", FieldAlignment.Left)
        };

        var updated = _storage.SetPlacements(projectFolder, withTemplate, placements);

        Assert.NotNull(updated.Template);
        Assert.Equal("t.png", updated.Template!.FileName);
    }

    [Fact]
    public void SetPlacements_NullProject_ThrowsArgumentNullException()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");

        Assert.Throws<ArgumentNullException>(() =>
            _storage.SetPlacements(
                projectFolder, null!, Array.Empty<FieldPlacement>()));
    }

    [Fact]
    public void SetPlacements_NullList_ThrowsArgumentNullException()
    {
        using var temp = new TempFolder();
        var projectFolder = Path.Combine(temp.Path, "MyProject");
        var project = _storage.Create(projectFolder, "Проект");

        Assert.Throws<ArgumentNullException>(() =>
            _storage.SetPlacements(projectFolder, project, null!));
    }

    [Fact]
    public void SetPlacements_ProjectFolderMissing_ThrowsDirectoryNotFoundException()
    {
        using var temp = new TempFolder();
        var nonExisting = Path.Combine(temp.Path, "НетТакой");

        var project = new Project(
            "Проект", DateTime.Now, string.Empty, Array.Empty<Field>());

        Assert.Throws<DirectoryNotFoundException>(() =>
            _storage.SetPlacements(
                nonExisting, project, Array.Empty<FieldPlacement>()));
    }

    // -----------------------------------------------------------------
    // Template / Placements — сериализация
    // -----------------------------------------------------------------

    [Fact]
    public void Serialize_ProjectWithoutTemplate_ContainsNullTemplate()
    {
        var project = new Project(
            "Проект", DateTime.Now, "data.xlsx", Array.Empty<Field>());

        var json = JsonProjectStorage.Serialize(project);

        Assert.Contains("template", json);
        Assert.Contains("null", json);
    }

    [Fact]
    public void Serialize_ProjectWithTemplate_ContainsValues()
    {
        var project = new Project(
            "Проект", DateTime.Now, "data.xlsx", Array.Empty<Field>(),
            template: new TemplateInfo("t.png", 100, 200));

        var json = JsonProjectStorage.Serialize(project);

        Assert.Contains("\"fileName\": \"t.png\"", json);
        Assert.Contains("\"width\": 100", json);
        Assert.Contains("\"height\": 200", json);
    }

    [Fact]
    public void Deserialize_OldJsonWithoutTemplate_ReturnsNull()
    {
        var json = """
        {
          "name": "Старый проект",
          "createdAt": "2026-01-01T00:00:00",
          "sourceFileName": "data.xlsx",
          "fields": [],
          "rowOverrides": []
        }
        """;

        var project = JsonProjectStorage.Deserialize(json);

        Assert.Null(project.Template);
        Assert.NotNull(project.Placements);
        Assert.Empty(project.Placements);
    }

    [Fact]
    public void Deserialize_JsonWithTemplateAndPlacements_RestoresThem()
    {
        var json = """
        {
          "name": "Проект",
          "createdAt": "2026-01-01T00:00:00",
          "sourceFileName": "data.xlsx",
          "fields": [],
          "rowOverrides": [],
          "template": {
            "fileName": "template.png",
            "width": 2480,
            "height": 3508
          },
          "placements": [
            {
              "fieldName": "ФИО",
              "x": 100,
              "y": 200,
              "fontSize": 48,
              "fontFamily": "Arial",
              "alignment": 1,
              "textColor": "#000000"
            }
          ]
        }
        """;

        var project = JsonProjectStorage.Deserialize(json);

        Assert.NotNull(project.Template);
        Assert.Equal("template.png", project.Template!.FileName);
        Assert.Equal(2480, project.Template.Width);
        Assert.Single(project.Placements);
        Assert.Equal("ФИО", project.Placements[0].FieldName);
        Assert.Equal(100, project.Placements[0].X);
        Assert.Equal(200, project.Placements[0].Y);
        Assert.Equal(48, project.Placements[0].FontSize);
        Assert.Equal("Arial", project.Placements[0].FontFamily);
    }
}