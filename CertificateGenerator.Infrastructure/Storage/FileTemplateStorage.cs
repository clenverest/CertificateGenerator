using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using SkiaSharp;

namespace CertificateGenerator.Infrastructure.Storage;

/// <summary>
/// Хранилище PNG-шаблона в папке проекта.
/// Шаблон всегда лежит в <c>template/template.png</c>.
/// </summary>
public sealed class FileTemplateStorage : ITemplateStorage
{
    /// <summary>
    /// Имя файла шаблона внутри папки template/.
    /// Одно на проект — при замене шаблона файл перезаписывается.
    /// </summary>
    internal const string TemplateFileName = "template.png";

    /// <summary>
    /// Имя папки шаблона внутри проекта.
    /// </summary>
    internal const string TemplateFolderName = "template";

    // ---------------------------------------------------------------------
    // ITemplateStorage
    // ---------------------------------------------------------------------

    public TemplateInfo SaveTemplate(string projectPath, string sourcePngPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException(
                "Путь к папке проекта не может быть пустым.",
                nameof(projectPath));
        }

        if (string.IsNullOrWhiteSpace(sourcePngPath))
        {
            throw new ArgumentException(
                "Путь к PNG-файлу не может быть пустым.",
                nameof(sourcePngPath));
        }

        if (!Directory.Exists(projectPath))
        {
            throw new DirectoryNotFoundException(
                $"Папка проекта \"{projectPath}\" не существует.");
        }

        if (!File.Exists(sourcePngPath))
        {
            throw new FileNotFoundException(
                $"Файл шаблона \"{sourcePngPath}\" не найден.",
                sourcePngPath);
        }

        // Прочитать размеры до копирования — чтобы не плодить файл,
        // если картинка окажется битой.
        var size = ReadPngSize(sourcePngPath, sourcePngPath);

        var templateFolder = Path.Combine(projectPath, TemplateFolderName);
        Directory.CreateDirectory(templateFolder);

        var destinationPath = Path.Combine(templateFolder, TemplateFileName);

        // Если исходник уже лежит внутри template/ под тем же именем —
        // File.Copy с overwrite сам с собой бросит IOException.
        // Тогда просто пропускаем копирование.
        var samePath = string.Equals(
            Path.GetFullPath(sourcePngPath),
            Path.GetFullPath(destinationPath),
            StringComparison.OrdinalIgnoreCase);

        if (!samePath)
        {
            File.Copy(sourcePngPath, destinationPath, overwrite: true);
        }

        return new TemplateInfo(TemplateFileName, size.Width, size.Height);
    }

    public TemplateInfo? GetTemplate(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException(
                "Путь к папке проекта не может быть пустым.",
                nameof(projectPath));
        }

        var templatePath = GetTemplatePath(projectPath);

        if (!File.Exists(templatePath))
        {
            return null;
        }

        var size = ReadPngSize(templatePath, templatePath);
        return new TemplateInfo(TemplateFileName, size.Width, size.Height);
    }

    public string GetTemplatePath(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException(
                "Путь к папке проекта не может быть пустым.",
                nameof(projectPath));
        }

        return Path.Combine(projectPath, TemplateFolderName, TemplateFileName);
    }

    // ---------------------------------------------------------------------
    // Чтение размеров PNG
    // ---------------------------------------------------------------------

    /// <summary>
    /// Прочитать ширину и высоту PNG в пикселях.
    /// </summary>
    /// <param name="imagePath">Путь к файлу картинки (для сообщения об ошибке).</param>
    /// <param name="decodePath">Путь, который читает SkiaSharp (обычно совпадает).</param>
    /// <exception cref="InvalidDataException">
    /// Бросается, если файл не удалось декодировать как картинку.
    /// </exception>
    private static (int Width, int Height) ReadPngSize(
        string imagePath,
        string decodePath)
    {
        using var bitmap = SKBitmap.Decode(decodePath);

        if (bitmap is null)
        {
            throw new InvalidDataException(
                $"Файл \"{imagePath}\" не является корректной картинкой " +
                "(не удалось декодировать).");
        }

        if (bitmap.Width <= 0 || bitmap.Height <= 0)
        {
            throw new InvalidDataException(
                $"Файл \"{imagePath}\" имеет некорректные размеры " +
                $"({bitmap.Width}×{bitmap.Height}).");
        }

        return (bitmap.Width, bitmap.Height);
    }
}