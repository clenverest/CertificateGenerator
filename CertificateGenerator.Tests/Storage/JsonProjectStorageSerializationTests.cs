using CertificateGenerator.Core.Models;
using CertificateGenerator.Infrastructure.Storage;

namespace CertificateGenerator.Tests.Storage;

public sealed class JsonProjectStorageSerializationTests
{
    // -----------------------------------------------------------------
    // Round-trip: Project -> JSON -> Project
    // -----------------------------------------------------------------

    [Fact]
    public void SerializeDeserialize_SimpleProject_AllPropertiesPreserved()
    {
        var createdAt = new DateTime(2026, 10, 12, 14, 30, 0);
        var original = new Project(
            "Осень 2026",
            createdAt,
            "data.xlsx",
            Array.Empty<Field>());

        var json = JsonProjectStorage.Serialize(original);
        var restored = JsonProjectStorage.Deserialize(json);

        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.CreatedAt, restored.CreatedAt);
        Assert.Equal(original.SourceFileName, restored.SourceFileName);
        Assert.Empty(restored.Fields);
    }

    [Fact]
    public void SerializeDeserialize_ProjectWithOneField_FieldPreserved()
    {
        var field = new Field(
            "ФИО",
            FieldKind.FullName,
            new[] { 0, 1, 2 },
            isRequired: true,
            separator: " ");
        var original = new Project(
            "Проект с полем",
            new DateTime(2026, 10, 12),
            "data.xlsx",
            new[] { field });

        var json = JsonProjectStorage.Serialize(original);
        var restored = JsonProjectStorage.Deserialize(json);

        Assert.Single(restored.Fields);
        var restoredField = restored.Fields[0];
        Assert.Equal("ФИО", restoredField.Name);
        Assert.Equal(FieldKind.FullName, restoredField.Kind);
        Assert.Equal(3, restoredField.SourceColumnIndexes.Count);
        Assert.Equal(0, restoredField.SourceColumnIndexes[0]);
        Assert.Equal(1, restoredField.SourceColumnIndexes[1]);
        Assert.Equal(2, restoredField.SourceColumnIndexes[2]);
        Assert.True(restoredField.IsRequired);
        Assert.Equal(" ", restoredField.Separator);
    }

    [Fact]
    public void SerializeDeserialize_ProjectWithMultipleFields_PreservesOrderAndData()
    {
        var original = new Project(
            "Проект",
            new DateTime(2026, 10, 12),
            "data.csv",
            new[]
            {
                new Field("ФИО", FieldKind.FullName, new[] { 0, 1 }),
                new Field("Класс", FieldKind.Simple, new[] { 2 }),
                new Field("Школа", FieldKind.Simple, new[] { 3 }, isRequired: true)
            });

        var json = JsonProjectStorage.Serialize(original);
        var restored = JsonProjectStorage.Deserialize(json);

        Assert.Equal(3, restored.Fields.Count);
        Assert.Equal("ФИО", restored.Fields[0].Name);
        Assert.Equal("Класс", restored.Fields[1].Name);
        Assert.Equal("Школа", restored.Fields[2].Name);
        Assert.True(restored.Fields[2].IsRequired);
    }

    // -----------------------------------------------------------------
    // Формат JSON
    // -----------------------------------------------------------------

    [Theory]
    [InlineData("ФИО")]
    [InlineData("Класс")]
    [InlineData("Школа №5")]
    public void Serialize_CyrillicName_NotEscapedInJson(string name)
    {
        var project = new Project(
            name,
            new DateTime(2026, 10, 12),
            "data.xlsx",
            Array.Empty<Field>());

        var json = JsonProjectStorage.Serialize(project);

        // Проверяем: имя поля в JSON присутствует как есть.
        Assert.Contains(name, json);

        // И не превратилось в escape-последовательность.
        // "\u" в JSON — начало escape-последовательности для Unicode.
        // Ни один из наших тестовых сценариев не должен её порождать.
        Assert.DoesNotContain("\\u04", json);
    }

    [Fact]
    public void Serialize_JsonIsIndented()
    {
        var project = new Project(
            "Проект",
            new DateTime(2026, 10, 12),
            "data.xlsx",
            Array.Empty<Field>());

        var json = JsonProjectStorage.Serialize(project);

        // С отступами JSON многострочный и содержит переносы строк.
        Assert.Contains("\n", json);

        // И как минимум два пробела подряд — отступ вложенных свойств.
        Assert.Contains("  ", json);
    }

    [Fact]
    public void Serialize_PropertyNamesInCamelCase()
    {
        var project = new Project(
            "Проект",
            new DateTime(2026, 10, 12),
            "data.xlsx",
            Array.Empty<Field>());

        var json = JsonProjectStorage.Serialize(project);

        // "name" в camelCase, а не "Name" в PascalCase.
        Assert.Contains("\"name\"", json);
        Assert.Contains("\"createdAt\"", json);
        Assert.Contains("\"sourceFileName\"", json);
        Assert.Contains("\"fields\"", json);
    }

    // -----------------------------------------------------------------
    // Защита от некорректных аргументов
    // -----------------------------------------------------------------

    [Fact]
    public void Serialize_NullProject_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            JsonProjectStorage.Serialize(null!));
    }

    [Fact]
    public void Deserialize_NullJson_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            JsonProjectStorage.Deserialize(null!));
    }

    [Fact]
    public void Deserialize_JsonLiteralNull_ThrowsInvalidDataException()
    {
        Assert.Throws<InvalidDataException>(() =>
            JsonProjectStorage.Deserialize("null"));
    }
}