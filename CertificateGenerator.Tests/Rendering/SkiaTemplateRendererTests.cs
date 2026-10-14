using CertificateGenerator.Core.Models;
using CertificateGenerator.Infrastructure.Rendering;
using SkiaSharp;

namespace CertificateGenerator.Tests.Rendering;

public sealed class SkiaTemplateRendererTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly SkiaTemplateRenderer _renderer = new();

    public SkiaTemplateRendererTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "CertificateGeneratorRendererTests_" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
            catch
            {
                // Ничего страшного — временная папка.
            }
        }
    }

    // ---------------------------------------------------------------------
    // Базовые сценарии
    // ---------------------------------------------------------------------

    [Fact]
    public void RenderPreview_NoPlacements_ReturnsTemplateAsPng()
    {
        var templatePath = CreateTemplate("empty.png", 400, 300);

        var result = _renderer.RenderPreview(
            templatePath,
            Array.Empty<FieldPlacement>(),
            new Dictionary<string, string>());

        Assert.NotNull(result);
        Assert.NotEmpty(result);

        var (width, height) = ReadSize(result);
        Assert.Equal(400, width);
        Assert.Equal(300, height);
    }

    [Fact]
    public void RenderPreview_NoMatchingFields_ReturnsTemplateAsPng()
    {
        var templatePath = CreateTemplate("nomatch.png", 400, 300);

        var placements = new[]
        {
            new FieldPlacement("ФИО", 50, 50, 24, "Arial", FieldAlignment.Left)
        };

        // fieldValues не содержит "ФИО" — поле пропускается.
        var values = new Dictionary<string, string>
        {
            ["Класс"] = "5А"
        };

        var result = _renderer.RenderPreview(templatePath, placements, values);

        var (width, height) = ReadSize(result);
        Assert.Equal(400, width);
        Assert.Equal(300, height);
    }

    [Fact]
    public void RenderPreview_EmptyValue_SkipsField()
    {
        var templatePath = CreateTemplate("emptyval.png", 400, 300);

        var placements = new[]
        {
            new FieldPlacement("ФИО", 50, 50, 24, "Arial", FieldAlignment.Left)
        };

        var values = new Dictionary<string, string>
        {
            ["ФИО"] = "   "  // пустое — пропускаем
        };

        var result = _renderer.RenderPreview(templatePath, placements, values);

        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData(FieldAlignment.Left)]
    [InlineData(FieldAlignment.Center)]
    [InlineData(FieldAlignment.Right)]
    public void RenderPreview_AllAlignments_Succeed(FieldAlignment alignment)
    {
        var templatePath = CreateTemplate("align.png", 400, 300);

        var placements = new[]
        {
            new FieldPlacement("ФИО", 200, 100, 24, "Arial", alignment)
        };

        var values = new Dictionary<string, string>
        {
            ["ФИО"] = "Иванов Иван"
        };

        var result = _renderer.RenderPreview(templatePath, placements, values);

        Assert.NotEmpty(result);
    }

    [Fact]
    public void RenderPreview_MultipleFields_DrawsAll()
    {
        var templatePath = CreateTemplate("multi.png", 600, 400);

        var placements = new[]
        {
            new FieldPlacement("ФИО", 100, 100, 32, "Arial", FieldAlignment.Center),
            new FieldPlacement("Класс", 100, 200, 24, "Arial", FieldAlignment.Left),
            new FieldPlacement("Школа", 500, 200, 24, "Arial", FieldAlignment.Right)
        };

        var values = new Dictionary<string, string>
        {
            ["ФИО"] = "Петров Пётр",
            ["Класс"] = "7Б",
            ["Школа"] = "Гимназия №1"
        };

        var result = _renderer.RenderPreview(templatePath, placements, values);

        var (width, height) = ReadSize(result);
        Assert.Equal(600, width);
        Assert.Equal(400, height);
    }

    [Fact]
    public void RenderPreview_TextOutsideTemplate_DoesNotThrow()
    {
        var templatePath = CreateTemplate("clip.png", 200, 200);

        var placements = new[]
        {
            // Текст уйдёт за границу — ClipRect должен обрезать, не упасть.
            new FieldPlacement("ФИО", 190, 190, 48, "Arial", FieldAlignment.Left)
        };

        var values = new Dictionary<string, string>
        {
            ["ФИО"] = "Очень длинное имя участника олимпиады"
        };

        var result = _renderer.RenderPreview(templatePath, placements, values);

        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData("#FF0000")]
    [InlineData("FF0000")]
    [InlineData("#80FF0000")]
    [InlineData("не цвет")]
    public void RenderPreview_VariousColors_DoesNotThrow(string color)
    {
        var templatePath = CreateTemplate("color.png", 200, 100);

        var placements = new[]
        {
            new FieldPlacement("ФИО", 20, 20, 16, "Arial", FieldAlignment.Left, color)
        };

        var values = new Dictionary<string, string>
        {
            ["ФИО"] = "Иванов"
        };

        var result = _renderer.RenderPreview(templatePath, placements, values);

        Assert.NotEmpty(result);
    }

    [Fact]
    public void RenderPreview_UnknownFont_DoesNotThrow()
    {
        var templatePath = CreateTemplate("font.png", 200, 100);

        var placements = new[]
        {
            new FieldPlacement("ФИО", 20, 20, 16, "Совершенно-неизвестный-шрифт",
                FieldAlignment.Left)
        };

        var values = new Dictionary<string, string>
        {
            ["ФИО"] = "Иванов"
        };

        var result = _renderer.RenderPreview(templatePath, placements, values);

        Assert.NotEmpty(result);
    }

    // ---------------------------------------------------------------------
    // Ошибки
    // ---------------------------------------------------------------------

    [Fact]
    public void RenderPreview_EmptyTemplatePath_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            _renderer.RenderPreview(
                string.Empty,
                Array.Empty<FieldPlacement>(),
                new Dictionary<string, string>()));
    }

    [Fact]
    public void RenderPreview_NullPlacements_Throws()
    {
        var templatePath = CreateTemplate("n.png", 100, 100);

        Assert.Throws<ArgumentNullException>(() =>
            _renderer.RenderPreview(
                templatePath,
                null!,
                new Dictionary<string, string>()));
    }

    [Fact]
    public void RenderPreview_NullFieldValues_Throws()
    {
        var templatePath = CreateTemplate("n.png", 100, 100);

        Assert.Throws<ArgumentNullException>(() =>
            _renderer.RenderPreview(
                templatePath,
                Array.Empty<FieldPlacement>(),
                null!));
    }

    [Fact]
    public void RenderPreview_TemplateMissing_Throws()
    {
        var missing = Path.Combine(_tempRoot, "no-such.png");

        Assert.Throws<FileNotFoundException>(() =>
            _renderer.RenderPreview(
                missing,
                Array.Empty<FieldPlacement>(),
                new Dictionary<string, string>()));
    }

    [Fact]
    public void RenderPreview_TemplateIsNotImage_Throws()
    {
        var fake = Path.Combine(_tempRoot, "fake.png");
        File.WriteAllText(fake, "Это не картинка.");

        Assert.Throws<InvalidDataException>(() =>
            _renderer.RenderPreview(
                fake,
                Array.Empty<FieldPlacement>(),
                new Dictionary<string, string>()));
    }

    // ---------------------------------------------------------------------
    // Помощники
    // ---------------------------------------------------------------------

    private string CreateTemplate(string fileName, int width, int height)
    {
        var path = Path.Combine(_tempRoot, fileName);

        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);

        return path;
    }

    private static (int Width, int Height) ReadSize(byte[] pngBytes)
    {
        using var data = SKData.CreateCopy(pngBytes);
        using var bitmap = SKBitmap.Decode(data);

        Assert.NotNull(bitmap);
        return (bitmap!.Width, bitmap.Height);
    }
}