using CertificateGenerator.Core.Models;
using CertificateGenerator.Infrastructure.Storage;
using SkiaSharp;

namespace CertificateGenerator.Tests.Storage;

public sealed class FileTemplateStorageTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly FileTemplateStorage _storage = new();

    public FileTemplateStorageTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "CertificateGeneratorTests_" + Guid.NewGuid().ToString("N"));

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
                // Тест уже прошёл или упал — чистить в любом случае не критично.
            }
        }
    }

    // ---------------------------------------------------------------------
    // SaveTemplate
    // ---------------------------------------------------------------------

    [Fact]
    public void SaveTemplate_CopiesFileAndReturnsSize()
    {
        var projectPath = CreateProjectFolder("Project1");
        var sourcePath = CreatePng("source.png", 800, 600);

        var info = _storage.SaveTemplate(projectPath, sourcePath);

        Assert.Equal("template.png", info.FileName);
        Assert.Equal(800, info.Width);
        Assert.Equal(600, info.Height);

        var expectedPath = Path.Combine(projectPath, "template", "template.png");
        Assert.True(File.Exists(expectedPath), "Файл должен быть скопирован.");
    }

    [Fact]
    public void SaveTemplate_OverwritesExistingTemplate()
    {
        var projectPath = CreateProjectFolder("Project2");
        var first = CreatePng("first.png", 100, 100);
        var second = CreatePng("second.png", 200, 150);

        _storage.SaveTemplate(projectPath, first);
        var info = _storage.SaveTemplate(projectPath, second);

        Assert.Equal(200, info.Width);
        Assert.Equal(150, info.Height);
    }

    [Fact]
    public void SaveTemplate_EmptyProjectPath_Throws()
    {
        var sourcePath = CreatePng("x.png", 10, 10);

        Assert.Throws<ArgumentException>(() =>
            _storage.SaveTemplate(string.Empty, sourcePath));
    }

    [Fact]
    public void SaveTemplate_EmptySourcePath_Throws()
    {
        var projectPath = CreateProjectFolder("Project3");

        Assert.Throws<ArgumentException>(() =>
            _storage.SaveTemplate(projectPath, string.Empty));
    }

    [Fact]
    public void SaveTemplate_ProjectFolderMissing_Throws()
    {
        var missingProject = Path.Combine(_tempRoot, "NoSuchFolder");
        var sourcePath = CreatePng("x.png", 10, 10);

        Assert.Throws<DirectoryNotFoundException>(() =>
            _storage.SaveTemplate(missingProject, sourcePath));
    }

    [Fact]
    public void SaveTemplate_SourceFileMissing_Throws()
    {
        var projectPath = CreateProjectFolder("Project4");
        var missingSource = Path.Combine(_tempRoot, "no-such.png");

        Assert.Throws<FileNotFoundException>(() =>
            _storage.SaveTemplate(projectPath, missingSource));
    }

    [Fact]
    public void SaveTemplate_NotAnImage_Throws()
    {
        var projectPath = CreateProjectFolder("Project5");
        var fakePath = Path.Combine(_tempRoot, "fake.png");
        File.WriteAllText(fakePath, "Это не PNG, а обычный текст.");

        Assert.Throws<InvalidDataException>(() =>
            _storage.SaveTemplate(projectPath, fakePath));
    }

    [Fact]
    public void SaveTemplate_SourceAlreadyInsideTemplateFolder_DoesNotThrow()
    {
        var projectPath = CreateProjectFolder("Project6");
        var templateFolder = Path.Combine(projectPath, "template");
        Directory.CreateDirectory(templateFolder);

        var existing = Path.Combine(templateFolder, "template.png");
        CreatePngAt(existing, 320, 240);

        // Пытаемся «скопировать» файл сам в себя.
        var info = _storage.SaveTemplate(projectPath, existing);

        Assert.Equal(320, info.Width);
        Assert.Equal(240, info.Height);
    }

    // ---------------------------------------------------------------------
    // GetTemplate
    // ---------------------------------------------------------------------

    [Fact]
    public void GetTemplate_NoTemplate_ReturnsNull()
    {
        var projectPath = CreateProjectFolder("Project7");

        var info = _storage.GetTemplate(projectPath);

        Assert.Null(info);
    }

    [Fact]
    public void GetTemplate_AfterSave_ReturnsSize()
    {
        var projectPath = CreateProjectFolder("Project8");
        var sourcePath = CreatePng("src.png", 640, 480);

        _storage.SaveTemplate(projectPath, sourcePath);
        var info = _storage.GetTemplate(projectPath);

        Assert.NotNull(info);
        Assert.Equal("template.png", info!.FileName);
        Assert.Equal(640, info.Width);
        Assert.Equal(480, info.Height);
    }

    [Fact]
    public void GetTemplate_EmptyProjectPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => _storage.GetTemplate(string.Empty));
    }

    // ---------------------------------------------------------------------
    // GetTemplatePath
    // ---------------------------------------------------------------------

    [Fact]
    public void GetTemplatePath_ReturnsExpectedPath()
    {
        var projectPath = CreateProjectFolder("Project9");

        var path = _storage.GetTemplatePath(projectPath);

        var expected = Path.Combine(projectPath, "template", "template.png");
        Assert.Equal(expected, path);
    }

    [Fact]
    public void GetTemplatePath_EmptyProjectPath_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            _storage.GetTemplatePath(string.Empty));
    }

    // ---------------------------------------------------------------------
    // Помощники
    // ---------------------------------------------------------------------

    private string CreateProjectFolder(string name)
    {
        var path = Path.Combine(_tempRoot, name);
        Directory.CreateDirectory(path);
        return path;
    }

    private string CreatePng(string fileName, int width, int height)
    {
        var path = Path.Combine(_tempRoot, fileName);
        CreatePngAt(path, width, height);
        return path;
    }

    private static void CreatePngAt(string path, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);

        // Заливаем белым — чтобы это был валидный PNG.
        canvas.Clear(SKColors.White);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
    }
}