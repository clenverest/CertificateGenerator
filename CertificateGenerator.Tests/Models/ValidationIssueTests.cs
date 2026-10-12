using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Tests.Models;

public sealed class ValidationIssueTests
{
    [Fact]
    public void Constructor_AllValuesStoredCorrectly()
    {
        var issue = new ValidationIssue(
            IssueSeverity.Error,
            5,
            "ФИО",
            "Значение не может быть пустым.");

        Assert.Equal(IssueSeverity.Error, issue.Severity);
        Assert.Equal(5, issue.RowIndex);
        Assert.Equal("ФИО", issue.FieldName);
        Assert.Equal("Значение не может быть пустым.", issue.Message);
    }

    [Fact]
    public void Constructor_NoRowIndex_Succeeds()
    {
        var issue = new ValidationIssue(
            IssueSeverity.Warning,
            ValidationIssue.NoRow,
            "Класс",
            "Поле не сопоставлено ни с одной колонкой.");

        Assert.Equal(ValidationIssue.NoRow, issue.RowIndex);
        Assert.Equal(IssueSeverity.Warning, issue.Severity);
    }

    [Fact]
    public void NoRow_EqualsMinusOne()
    {
        Assert.Equal(-1, ValidationIssue.NoRow);
    }
}