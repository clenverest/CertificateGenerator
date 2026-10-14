namespace CertificateGenerator.Core.Models;

/// <summary>
/// Информация о PNG-шаблоне проекта.
/// Хранится в project.json, сам файл лежит в папке template/.
/// </summary>
public sealed class TemplateInfo
{
    /// <summary>Имя файла внутри папки template/ (например, "template.png").</summary>
    public string FileName { get; }

    /// <summary>Ширина картинки в пикселях.</summary>
    public int Width { get; }

    /// <summary>Высота картинки в пикселях.</summary>
    public int Height { get; }

    /// <summary>
    /// Создать информацию о шаблоне.
    /// </summary>
    /// <param name="fileName">Имя файла внутри template/.</param>
    /// <param name="width">Ширина в пикселях, больше нуля.</param>
    /// <param name="height">Высота в пикселях, больше нуля.</param>
    /// <exception cref="ArgumentException">
    /// Бросается, если <paramref name="fileName"/> пуст.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Бросается, если <paramref name="width"/> или <paramref name="height"/>
    /// не больше нуля.
    /// </exception>
    public TemplateInfo(string fileName, int width, int height)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "Имя файла шаблона не может быть пустым.",
                nameof(fileName));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Ширина шаблона должна быть больше нуля.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height),
                "Высота шаблона должна быть больше нуля.");
        }

        FileName = fileName;
        Width = width;
        Height = height;
    }
}