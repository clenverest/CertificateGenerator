using CertificateGenerator.Core.Models;
using CertificateGenerator.Core.Services;

namespace CertificateGenerator.Tests.Services;

public class ColumnSuggesterTests
{
    private static ColumnInfo Col(int index, string name) => new(index, name);

    // ---------------------------------------------------------------------
    // SuggestSimple
    // ---------------------------------------------------------------------

    [Fact]
    public void SuggestSimple_ExactMatch_ReturnsIndex()
    {
        var columns = new[] { Col(0, "ФИО"), Col(1, "Класс"), Col(2, "Школа") };

        var result = ColumnSuggester.SuggestSimple("Класс", columns);

        Assert.Single(result);
        Assert.Equal(1, result[0]);
    }

    [Fact]
    public void SuggestSimple_CaseInsensitive_ReturnsIndex()
    {
        var columns = new[] { Col(0, "Класс") };

        var result = ColumnSuggester.SuggestSimple("класс", columns);

        Assert.Single(result);
        Assert.Equal(0, result[0]);
    }

    [Fact]
    public void SuggestSimple_YoToYe_ReturnsIndex()
    {
        var columns = new[] { Col(0, "Учёт") };

        var result = ColumnSuggester.SuggestSimple("учет", columns);

        Assert.Single(result);
        Assert.Equal(0, result[0]);
    }

    [Fact]
    public void SuggestSimple_TrimsWhitespace_ReturnsIndex()
    {
        var columns = new[] { Col(0, "Класс") };

        var result = ColumnSuggester.SuggestSimple("  Класс  ", columns);

        Assert.Single(result);
        Assert.Equal(0, result[0]);
    }

    [Fact]
    public void SuggestSimple_NoMatch_ReturnsEmpty()
    {
        var columns = new[] { Col(0, "ФИО"), Col(1, "Школа") };

        var result = ColumnSuggester.SuggestSimple("Класс", columns);

        Assert.Empty(result);
    }

    [Fact]
    public void SuggestSimple_MultipleMatches_ReturnsEmpty()
    {
        var columns = new[] { Col(0, "Класс"), Col(1, "класс") };

        var result = ColumnSuggester.SuggestSimple("Класс", columns);

        Assert.Empty(result);
    }

    [Fact]
    public void SuggestSimple_EmptyFieldName_ReturnsEmpty()
    {
        var columns = new[] { Col(0, "Класс") };

        var result = ColumnSuggester.SuggestSimple("", columns);

        Assert.Empty(result);
    }

    [Fact]
    public void SuggestSimple_EmptyColumns_ReturnsEmpty()
    {
        var result = ColumnSuggester.SuggestSimple("Класс", Array.Empty<ColumnInfo>());

        Assert.Empty(result);
    }

    // ---------------------------------------------------------------------
    // SuggestFullName — одна колонка «ФИО»
    // ---------------------------------------------------------------------

    [Fact]
    public void SuggestFullName_SingleFullNameColumn_ReturnsIt()
    {
        var columns = new[] { Col(0, "ФИО"), Col(1, "Класс") };

        var result = ColumnSuggester.SuggestFullName(columns);

        Assert.Single(result);
        Assert.Equal(0, result[0]);
    }

    [Fact]
    public void SuggestFullName_FullNameLowerCase_ReturnsIt()
    {
        var columns = new[] { Col(0, "фио"), Col(1, "Класс") };

        var result = ColumnSuggester.SuggestFullName(columns);

        Assert.Single(result);
        Assert.Equal(0, result[0]);
    }

    // ---------------------------------------------------------------------
    // SuggestFullName — три колонки
    // ---------------------------------------------------------------------

    [Fact]
    public void SuggestFullName_ThreeColumns_ReturnsInOrder()
    {
        var columns = new[]
        {
            Col(0, "Фамилия"),
            Col(1, "Имя"),
            Col(2, "Отчество"),
            Col(3, "Класс")
        };

        var result = ColumnSuggester.SuggestFullName(columns);

        Assert.Equal(3, result.Count);
        Assert.Equal(0, result[0]);
        Assert.Equal(1, result[1]);
        Assert.Equal(2, result[2]);
    }

    [Fact]
    public void SuggestFullName_OrderDoesNotMatter()
    {
        var columns = new[]
        {
            Col(0, "Отчество"),
            Col(1, "Фамилия"),
            Col(2, "Имя")
        };

        var result = ColumnSuggester.SuggestFullName(columns);

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0]); // фамилия
        Assert.Equal(2, result[1]); // имя
        Assert.Equal(0, result[2]); // отчество
    }

    [Fact]
    public void SuggestFullName_SynonymsAreRecognized()
    {
        var columns = new[]
        {
            Col(0, "Фамилия ученика"),
            Col(1, "Имя участника"),
            Col(2, "Отчество ученика")
        };

        var result = ColumnSuggester.SuggestFullName(columns);

        Assert.Equal(3, result.Count);
        Assert.Equal(0, result[0]);
        Assert.Equal(1, result[1]);
        Assert.Equal(2, result[2]);
    }

    [Fact]
    public void SuggestFullName_OnlyTwoOfThree_ReturnsEmpty()
    {
        var columns = new[]
        {
            Col(0, "Фамилия"),
            Col(1, "Имя"),
            Col(2, "Класс")
        };

        var result = ColumnSuggester.SuggestFullName(columns);

        Assert.Empty(result);
    }

    [Fact]
    public void SuggestFullName_DuplicateSurname_ReturnsEmpty()
    {
        var columns = new[]
        {
            Col(0, "Фамилия"),
            Col(1, "Фамилия"),
            Col(2, "Имя"),
            Col(3, "Отчество")
        };

        var result = ColumnSuggester.SuggestFullName(columns);

        Assert.Empty(result);
    }

    [Fact]
    public void SuggestFullName_FullNameColumnTakesPriorityOverThree()
    {
        // Если есть и «ФИО», и три колонки — предпочитаем «ФИО».
        var columns = new[]
        {
            Col(0, "ФИО"),
            Col(1, "Фамилия"),
            Col(2, "Имя"),
            Col(3, "Отчество")
        };

        var result = ColumnSuggester.SuggestFullName(columns);

        Assert.Single(result);
        Assert.Equal(0, result[0]);
    }

    [Fact]
    public void SuggestFullName_EmptyColumns_ReturnsEmpty()
    {
        var result = ColumnSuggester.SuggestFullName(Array.Empty<ColumnInfo>());

        Assert.Empty(result);
    }

    [Fact]
    public void SuggestFullName_NothingMatches_ReturnsEmpty()
    {
        var columns = new[] { Col(0, "Класс"), Col(1, "Школа") };

        var result = ColumnSuggester.SuggestFullName(columns);

        Assert.Empty(result);
    }
}