using CertificateGenerator.Core.Services;

namespace CertificateGenerator.Tests.Services;

public sealed class FileNameUniquifierTests
{
    [Fact]
    public void MakeUnique_FreeName_ReturnsAsIs()
    {
        var result = FileNameUniquifier.MakeUnique(
            "Иванов Иван.pdf",
            Array.Empty<string>());

        Assert.Equal("Иванов Иван.pdf", result);
    }

    [Fact]
    public void MakeUnique_TakenOnce_AddsSuffix2()
    {
        var result = FileNameUniquifier.MakeUnique(
            "Иванов Иван.pdf",
            new[] { "Иванов Иван.pdf" });

        Assert.Equal("Иванов Иван_2.pdf", result);
    }

    [Fact]
    public void MakeUnique_TakenTwice_AddsSuffix3()
    {
        var result = FileNameUniquifier.MakeUnique(
            "Иванов Иван.pdf",
            new[] { "Иванов Иван.pdf", "Иванов Иван_2.pdf" });

        Assert.Equal("Иванов Иван_3.pdf", result);
    }

    [Fact]
    public void MakeUnique_CaseInsensitive()
    {
        var result = FileNameUniquifier.MakeUnique(
            "Иванов Иван.pdf",
            new[] { "иванов иван.PDF" });

        Assert.Equal("Иванов Иван_2.pdf", result);
    }

    [Fact]
    public void MakeUnique_DifferentExtension_Independent()
    {
        var result = FileNameUniquifier.MakeUnique(
            "Иванов.pdf",
            new[] { "Иванов.docx" });

        Assert.Equal("Иванов.pdf", result);
    }

    [Fact]
    public void MakeUnique_EmptyDesiredName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            FileNameUniquifier.MakeUnique(
                string.Empty,
                Array.Empty<string>()));
    }

    [Fact]
    public void MakeUnique_NullExisting_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FileNameUniquifier.MakeUnique(
                "Иванов.pdf",
                null!));
    }

    [Fact]
    public void MakeUnique_TwoCollisions_ReturnsThirdFree()
    {
        // Свободных имён: «Петров.pdf» и «Петров_3.pdf».
        // «Петров_2.pdf» занято.
        var taken = new[] { "Петров.pdf", "Петров_2.pdf" };

        var result = FileNameUniquifier.MakeUnique("Петров.pdf", taken);

        Assert.Equal("Петров_3.pdf", result);
    }
}