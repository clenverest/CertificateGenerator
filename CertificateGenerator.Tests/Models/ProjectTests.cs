using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Tests.Models;

public sealed class ProjectTests
{
    [Fact]
    public void Constructor_AllValuesStoredCorrectly()
    {
        var createdAt = new DateTime(2026, 10, 12, 14, 30, 0);
        var fields = new[]
        {
            new Field("ФИО", FieldKind.FullName, new[] { 0, 1, 2 }, isRequired: true),
            new Field("Класс", FieldKind.Simple, new[] { 3 })
        };

        var project = new Project("Осень 2026", createdAt, "data.xlsx", fields);

        Assert.Equal("Осень 2026", project.Name);
        Assert.Equal(createdAt, project.CreatedAt);
        Assert.Equal("data.xlsx", project.SourceFileName);
        Assert.Equal(2, project.Fields.Count);
        Assert.Equal("ФИО", project.Fields[0].Name);
        Assert.Equal("Класс", project.Fields[1].Name);
    }

    [Fact]
    public void Constructor_EmptyFieldsList_Succeeds()
    {
        var project = new Project(
            "Пустой проект",
            new DateTime(2026, 10, 12),
            "data.csv",
            Array.Empty<Field>());

        Assert.Empty(project.Fields);
    }

    [Fact]
    public void Constructor_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Project(string.Empty, DateTime.Now, "data.xlsx", Array.Empty<Field>()));
    }

    [Fact]
    public void Constructor_NullFields_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Project("Проект", DateTime.Now, "data.xlsx", null!));
    }

    [Fact]
    public void Constructor_EmptySourceFileName_Succeeds()
    {
        var project = new Project(
            "Проект до загрузки таблицы",
            new DateTime(2026, 10, 12),
            string.Empty,
            Array.Empty<Field>());

        Assert.Equal(string.Empty, project.SourceFileName);
    }

    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var project = new Project(
            "Олимпиада 2026",
            new DateTime(2026, 1, 1),
            "",
            Array.Empty<Field>());

        Assert.Equal("Олимпиада 2026", project.Name);
        Assert.Equal("", project.SourceFileName);
        Assert.Empty(project.Fields);
    }

    [Fact]
    public void Create_WithNullSourceFileName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Project("Проект", DateTime.Now, null!, Array.Empty<Field>()));
    }

    [Fact]
    public void Create_WithNullFields_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Project("Проект", DateTime.Now, "", null!));
    }

    [Fact]
    public void Create_WithEmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Project("", DateTime.Now, "", Array.Empty<Field>()));
    }

    [Fact]
    public void Create_WithWhitespaceName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Project("   ", DateTime.Now, "", Array.Empty<Field>()));
    }

    [Fact]
    public void Create_WithFields_StoresThem()
    {
        var fields = new[]
        {
            new Field("Класс", FieldKind.Simple, new[] { 0 }),
            new Field("ФИО", FieldKind.FullName, new[] { 1, 2 })
        };

        var project = new Project("Проект", DateTime.Now, "", fields);

        Assert.Equal(2, project.Fields.Count);
    }

    // ---------------------------------------------------------------------
    // RowOverrides
    // ---------------------------------------------------------------------

    [Fact]
    public void Create_WithoutRowOverrides_EmptyList()
    {
        var project = new Project("Проект", DateTime.Now, "", Array.Empty<Field>());

        Assert.Empty(project.RowOverrides);
    }

    [Fact]
    public void Create_WithNullRowOverrides_EmptyList()
    {
        var project = new Project(
            "Проект", DateTime.Now, "", Array.Empty<Field>(),
            rowOverrides: null);

        Assert.Empty(project.RowOverrides);
    }

    [Fact]
    public void Create_WithRowOverrides_StoresThem()
    {
        var overrides = new[]
        {
            new RowOverride(0, new[] { "Иванов", "Иван" }),
            new RowOverride(2, new[] { "Петров", "Пётр" })
        };

        var project = new Project(
            "Проект", DateTime.Now, "", Array.Empty<Field>(),
            rowOverrides: overrides);

        Assert.Equal(2, project.RowOverrides.Count);
        Assert.Equal(0, project.RowOverrides[0].RowIndex);
        Assert.Equal(2, project.RowOverrides[1].RowIndex);
    }

    [Fact]
    public void Create_CopiesRowOverrides()
    {
        // Если снаружи изменят список — Project не должен это увидеть.
        var overrides = new List<RowOverride>
        {
            new RowOverride(0, new[] { "Иванов" })
        };

        var project = new Project(
            "Проект", DateTime.Now, "", Array.Empty<Field>(),
            rowOverrides: overrides);

        overrides.Add(new RowOverride(1, new[] { "Петров" }));

        Assert.Single(project.RowOverrides);
    }

    // ---------------------------------------------------------------------
    // Template и Placements
    // ---------------------------------------------------------------------

    [Fact]
    public void Create_WithoutTemplate_TemplateIsNull()
    {
        var project = new Project("Проект", DateTime.Now, "", Array.Empty<Field>());

        Assert.Null(project.Template);
    }

    [Fact]
    public void Create_WithoutPlacements_EmptyList()
    {
        var project = new Project("Проект", DateTime.Now, "", Array.Empty<Field>());

        Assert.Empty(project.Placements);
    }

    [Fact]
    public void Create_WithNullPlacements_EmptyList()
    {
        var project = new Project(
            "Проект", DateTime.Now, "", Array.Empty<Field>(),
            placements: null);

        Assert.Empty(project.Placements);
    }

    [Fact]
    public void Create_WithTemplate_StoresIt()
    {
        var template = new TemplateInfo("template.png", 2480, 3508);

        var project = new Project(
            "Проект", DateTime.Now, "", Array.Empty<Field>(),
            template: template);

        Assert.Same(template, project.Template);
        Assert.Equal("template.png", project.Template!.FileName);
        Assert.Equal(2480, project.Template.Width);
        Assert.Equal(3508, project.Template.Height);
    }

    [Fact]
    public void Create_WithPlacements_StoresThem()
    {
        var placements = new[]
        {
            new FieldPlacement("ФИО", 100, 200, 48, "Arial", FieldAlignment.Center),
            new FieldPlacement("Класс", 100, 300, 32, "Arial", FieldAlignment.Left)
        };

        var project = new Project(
            "Проект", DateTime.Now, "", Array.Empty<Field>(),
            placements: placements);

        Assert.Equal(2, project.Placements.Count);
        Assert.Equal("ФИО", project.Placements[0].FieldName);
        Assert.Equal("Класс", project.Placements[1].FieldName);
    }

    [Fact]
    public void Create_CopiesPlacements()
    {
        // Если снаружи изменят список — Project не должен это увидеть.
        var placements = new List<FieldPlacement>
        {
            new FieldPlacement("ФИО", 100, 200, 48, "Arial", FieldAlignment.Center)
        };

        var project = new Project(
            "Проект", DateTime.Now, "", Array.Empty<Field>(),
            placements: placements);

        placements.Add(
            new FieldPlacement("Класс", 100, 300, 32, "Arial", FieldAlignment.Left));

        Assert.Single(project.Placements);
    }

    [Fact]
    public void Create_WithAllOptionalParameters_StoresAll()
    {
        var template = new TemplateInfo("t.png", 100, 200);
        var placements = new[]
        {
            new FieldPlacement("ФИО", 10, 20, 30, "Arial", FieldAlignment.Left)
        };
        var overrides = new[]
        {
            new RowOverride(0, new[] { "Иванов" })
        };

        var project = new Project(
            "Полный", DateTime.Now, "data.xlsx", Array.Empty<Field>(),
            rowOverrides: overrides,
            template: template,
            placements: placements);

        Assert.Single(project.RowOverrides);
        Assert.NotNull(project.Template);
        Assert.Single(project.Placements);
    }
}