using CertificateGenerator.Core.Models;
using CertificateGenerator.Infrastructure.Generators;
using CertificateGenerator.Tests.Fakes;
using SkiaSharp;

namespace CertificateGenerator.Tests.Generators;

public sealed class QuestPdfGeneratorTests
{
    static QuestPdfGeneratorTests()
    {
        // QuestPDF требует установить лицензию до первого GeneratePdf.
        // В приложении это делает App.xaml.cs, в тестах — здесь.
        QuestPDF.Settings.License =
            QuestPDF.Infrastructure.LicenseType.Community;
    }

    /// <summary>
    /// Сгенерировать маленький PNG-шаблон указанного размера.
    /// </summary>
    private static byte[] MakeTestPng(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        return data.ToArray();
    }

    /// <summary>
    /// Собрать QuestPdfGenerator с заданным рендерером.
    /// </summary>
    private static QuestPdfGenerator CreateGenerator(
        FakeTemplateRenderer renderer)
    {
        return new QuestPdfGenerator(renderer);
    }

    [Fact]
    public void Constructor_NullRenderer_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new QuestPdfGenerator(null!));
    }

    [Fact]
    public void GenerateCertificate_ReturnsNonEmptyBytes()
    {
        var renderer = new FakeTemplateRenderer
        {
            BytesToReturn = MakeTestPng(200, 100)
        };
        var generator = CreateGenerator(renderer);

        var pdf = generator.GenerateCertificate(
            "template.png",
            Array.Empty<FieldPlacement>(),
            new Dictionary<string, string>());

        Assert.NotNull(pdf);
        Assert.NotEmpty(pdf);
    }

    [Fact]
    public void GenerateCertificate_StartsWithPdfHeader()
    {
        var renderer = new FakeTemplateRenderer
        {
            BytesToReturn = MakeTestPng(200, 100)
        };
        var generator = CreateGenerator(renderer);

        var pdf = generator.GenerateCertificate(
            "template.png",
            Array.Empty<FieldPlacement>(),
            new Dictionary<string, string>());

        // Все PDF начинаются с "%PDF-" (5 байт: 0x25 0x50 0x44 0x46 0x2D).
        Assert.True(pdf.Length >= 5);
        Assert.Equal(0x25, pdf[0]);  // '%'
        Assert.Equal(0x50, pdf[1]);  // 'P'
        Assert.Equal(0x44, pdf[2]);  // 'D'
        Assert.Equal(0x46, pdf[3]);  // 'F'
        Assert.Equal(0x2D, pdf[4]);  // '-'
    }

    [Fact]
    public void GenerateCertificate_PassesArgumentsToRenderer()
    {
        var renderer = new FakeTemplateRenderer
        {
            BytesToReturn = MakeTestPng(200, 100)
        };
        var generator = CreateGenerator(renderer);

        var placements = new List<FieldPlacement>
        {
            new("ФИО", 100, 200, 48, "Arial", FieldAlignment.Center)
        };
        var values = new Dictionary<string, string> { ["ФИО"] = "Иванов Иван" };

        generator.GenerateCertificate("template.png", placements, values);

        Assert.Single(renderer.Calls);
        var call = renderer.Calls[0];
        Assert.Equal("template.png", call.TemplatePath);
        Assert.Same(placements, call.Placements);
        Assert.Same(values, call.FieldValues);
    }

    [Fact]
    public void GenerateCertificate_InvalidPng_Throws()
    {
        var renderer = new FakeTemplateRenderer
        {
            BytesToReturn = new byte[] { 1, 2, 3 }  // не PNG
        };
        var generator = CreateGenerator(renderer);

        Assert.Throws<InvalidDataException>(() =>
            generator.GenerateCertificate(
                "template.png",
                Array.Empty<FieldPlacement>(),
                new Dictionary<string, string>()));
    }

    [Fact]
    public void GenerateCertificate_RendererThrows_Propagates()
    {
        var expected = new FileNotFoundException("Шаблон не найден.");
        var renderer = new FakeTemplateRenderer
        {
            ExceptionToThrow = expected
        };
        var generator = CreateGenerator(renderer);

        var actual = Assert.Throws<FileNotFoundException>(() =>
            generator.GenerateCertificate(
                "template.png",
                Array.Empty<FieldPlacement>(),
                new Dictionary<string, string>()));

        Assert.Same(expected, actual);
    }

    [Fact]
    public void GenerateCertificate_EmptyTemplatePath_Throws()
    {
        var renderer = new FakeTemplateRenderer
        {
            BytesToReturn = MakeTestPng(200, 100)
        };
        var generator = CreateGenerator(renderer);

        Assert.Throws<ArgumentException>(() =>
            generator.GenerateCertificate(
                string.Empty,
                Array.Empty<FieldPlacement>(),
                new Dictionary<string, string>()));
    }
}