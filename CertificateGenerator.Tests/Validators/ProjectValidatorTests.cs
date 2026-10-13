using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using CertificateGenerator.Core.Validators;
using CertificateGenerator.Tests.Fakes;

namespace CertificateGenerator.Tests.Validators;

public class ProjectValidatorTests
{
    private static ColumnInfo Col(int index, string name) => new(index, name);

    private static TableData SampleTable()
    {
        return new TableData(
            new[] { Col(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });
    }

    private static ValidationIssue Issue(IssueSeverity severity, string fieldName)
    {
        return new ValidationIssue(severity, 0, fieldName, "test");
    }

    // ---------------------------------------------------------------------
    // Пустой список валидаторов
    // ---------------------------------------------------------------------

    [Fact]
    public void EmptyValidators_ReturnsEmpty()
    {
        var validator = new ProjectValidator(Array.Empty<IValidator>());

        var result = validator.Validate(SampleTable(), Array.Empty<Field>());

        Assert.Empty(result);
    }

    // ---------------------------------------------------------------------
    // Один валидатор
    // ---------------------------------------------------------------------

    [Fact]
    public void OneValidator_ReturnsItsIssues()
    {
        var fake = new FakeValidator
        {
            IssuesToReturn = new[]
            {
                Issue(IssueSeverity.Error, "ФИО"),
                Issue(IssueSeverity.Warning, "Класс")
            }
        };
        var validator = new ProjectValidator(new IValidator[] { fake });

        var result = validator.Validate(SampleTable(), Array.Empty<Field>());

        Assert.Equal(2, result.Count);
        Assert.Equal("ФИО", result[0].FieldName);
        Assert.Equal("Класс", result[1].FieldName);
    }

    [Fact]
    public void OneValidator_CalledOnce()
    {
        var fake = new FakeValidator();
        var validator = new ProjectValidator(new IValidator[] { fake });
        var table = SampleTable();
        var fields = Array.Empty<Field>();

        validator.Validate(table, fields);

        Assert.Single(fake.Calls);
        Assert.Same(table, fake.Calls[0].Table);
        Assert.Same(fields, fake.Calls[0].Fields);
    }

    // ---------------------------------------------------------------------
    // Несколько валидаторов
    // ---------------------------------------------------------------------

    [Fact]
    public void MultipleValidators_ConcatenatesInOrder()
    {
        var first = new FakeValidator
        {
            IssuesToReturn = new[] { Issue(IssueSeverity.Error, "A") }
        };
        var second = new FakeValidator
        {
            IssuesToReturn = new[]
            {
                Issue(IssueSeverity.Warning, "B"),
                Issue(IssueSeverity.Warning, "C")
            }
        };
        var validator = new ProjectValidator(new IValidator[] { first, second });

        var result = validator.Validate(SampleTable(), Array.Empty<Field>());

        Assert.Equal(3, result.Count);
        Assert.Equal("A", result[0].FieldName);
        Assert.Equal("B", result[1].FieldName);
        Assert.Equal("C", result[2].FieldName);
    }

    [Fact]
    public void MultipleValidators_AllCalled()
    {
        var first = new FakeValidator();
        var second = new FakeValidator();
        var third = new FakeValidator();
        var validator = new ProjectValidator(new IValidator[] { first, second, third });

        validator.Validate(SampleTable(), Array.Empty<Field>());

        Assert.Single(first.Calls);
        Assert.Single(second.Calls);
        Assert.Single(third.Calls);
    }

    [Fact]
    public void MultipleValidators_SomeReturnEmpty()
    {
        var first = new FakeValidator
        {
            IssuesToReturn = new[] { Issue(IssueSeverity.Error, "A") }
        };
        var second = new FakeValidator
        {
            IssuesToReturn = Array.Empty<ValidationIssue>()
        };
        var third = new FakeValidator
        {
            IssuesToReturn = new[] { Issue(IssueSeverity.Warning, "C") }
        };
        var validator = new ProjectValidator(new IValidator[] { first, second, third });

        var result = validator.Validate(SampleTable(), Array.Empty<Field>());

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].FieldName);
        Assert.Equal("C", result[1].FieldName);
    }

    // ---------------------------------------------------------------------
    // CreateDefault
    // ---------------------------------------------------------------------

    [Fact]
    public void CreateDefault_IncludesAllThreeValidators()
    {
        var validator = ProjectValidator.CreateDefault();

        // Проверяем через поведение: пустая таблица, обязательное поле пустое,
        // дубликат, пробелы — все три валидатора дадут проблемы.
        var table = new TableData(
            new[]
            {
                Col(0, "ФИО"),
                Col(1, "Класс")
            },
            new[]
            {
                (IReadOnlyList<string>)new[] { "  Иванов", "5А" },
                (IReadOnlyList<string>)new[] { "  Иванов", "" }
            });

        var fio = new Field(
            "ФИО", FieldKind.Simple, new[] { 0 });
        var klass = new Field(
            "Класс", FieldKind.Simple, new[] { 1 }, isRequired: true);

        var result = validator.Validate(table, new[] { fio, klass });

        // Проверяем, что проблемы разных типов есть.
        Assert.NotEmpty(result);

        // RequiredFieldValidator: Класс пустой во второй строке.
        Assert.Contains(result, i =>
            i.Severity == IssueSeverity.Error && i.FieldName == "Класс");

        // DuplicateValidator: «Иванов» в ФИО дважды.
        Assert.Contains(result, i =>
            i.Severity == IssueSeverity.Warning && i.FieldName == "ФИО");

        // WhitespaceValidator: пробелы в «  Иванов».
        Assert.Contains(result, i =>
            i.FieldName == "ФИО" && i.Message.Contains("пробелы"));
    }

    // ---------------------------------------------------------------------
    // Null-проверки
    // ---------------------------------------------------------------------

    [Fact]
    public void Constructor_NullValidators_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProjectValidator(null!));
    }

    [Fact]
    public void Validate_NullTable_Throws()
    {
        var validator = ProjectValidator.CreateDefault();

        Assert.Throws<ArgumentNullException>(() =>
            validator.Validate(null!, Array.Empty<Field>()));
    }

    [Fact]
    public void Validate_NullFields_Throws()
    {
        var validator = ProjectValidator.CreateDefault();

        Assert.Throws<ArgumentNullException>(() =>
            validator.Validate(SampleTable(), null!));
    }
}