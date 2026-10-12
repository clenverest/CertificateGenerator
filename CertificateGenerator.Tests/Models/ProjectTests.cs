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
}