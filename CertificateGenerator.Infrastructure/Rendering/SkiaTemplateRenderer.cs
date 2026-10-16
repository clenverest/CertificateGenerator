using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using SkiaSharp;

namespace CertificateGenerator.Infrastructure.Rendering;

/// <summary>
/// Рендер превью грамоты: наложение значений полей на PNG-шаблон.
/// Координаты (X, Y) — левый верхний угол текста в пикселях.
/// </summary>
public sealed class SkiaTemplateRenderer : ITemplateRenderer
{
    public byte[] RenderPreview(
        string templatePath,
        IReadOnlyList<FieldPlacement> placements,
        IReadOnlyDictionary<string, string> fieldValues)
    {
        if (string.IsNullOrWhiteSpace(templatePath))
        {
            throw new ArgumentException(
                "Путь к шаблону не может быть пустым.",
                nameof(templatePath));
        }

        if (placements is null)
        {
            throw new ArgumentNullException(nameof(placements));
        }

        if (fieldValues is null)
        {
            throw new ArgumentNullException(nameof(fieldValues));
        }

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException(
                $"Файл шаблона \"{templatePath}\" не найден.",
                templatePath);
        }

        // Шаблон — это холст, на который накладываем текст.
        // Decode возвращает null, если файл битый или не картинка.
        using var bitmap = SKBitmap.Decode(templatePath);

        if (bitmap is null)
        {
            throw new InvalidDataException(
                $"Файл \"{templatePath}\" не является корректной картинкой.");
        }

        using var canvas = new SKCanvas(bitmap);

        // Обрезаем всё, что вылезает за границы шаблона.
        canvas.ClipRect(new SKRect(0, 0, bitmap.Width, bitmap.Height));

        foreach (var placement in placements)
        {
            DrawPlacement(canvas, bitmap, placement, fieldValues);
        }

        return EncodeToPng(bitmap);
    }

    // ---------------------------------------------------------------------
    // Рисование одного поля
    // ---------------------------------------------------------------------

    private static void DrawPlacement(
        SKCanvas canvas,
        SKBitmap bitmap,
        FieldPlacement placement,
        IReadOnlyDictionary<string, string> fieldValues)
    {
        // Нет значения — нет текста. Пропускаем.
        if (!fieldValues.TryGetValue(placement.FieldName, out var value))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var color = ParseColor(placement.TextColor);
        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true
        };

        using var font = new SKFont(
            SKTypeface.FromFamilyName(placement.FontFamily),
            placement.FontSize);

        // Ширина текста — нужна для Center и Right.
        var textWidth = font.MeasureText(value);

        // Эффективный X: если «по центру шаблона» — игнорируем placement.X.
        var effectiveX = placement.IsCenteredHorizontally
            ? bitmap.Width / 2f
            : placement.X;

        var drawX = placement.Alignment switch
        {
            FieldAlignment.Center => effectiveX - textWidth / 2f,
            FieldAlignment.Right => effectiveX - textWidth,
            _ => effectiveX
        };

        // SkiaSharp рисует от базовой линии. Наш Y — верхний край текста.
        // Сдвигаем вниз на высоту «над базовой линией».
        var metrics = font.Metrics;
        var drawY = placement.Y - metrics.Ascent;  // ascent отрицательный

        canvas.DrawText(value, drawX, drawY, font, paint);
    }

    // ---------------------------------------------------------------------
    // Цвет
    // ---------------------------------------------------------------------

    /// <summary>
    /// Разобрать цвет из строки вида "#RRGGBB" или "#AARRGGBB".
    /// Если не получилось — вернуть чёрный.
    /// </summary>
    private static SKColor ParseColor(string textColor)
    {
        if (string.IsNullOrWhiteSpace(textColor))
        {
            return SKColors.Black;
        }

        var hex = textColor.TrimStart('#');

        if (hex.Length == 6 &&
            uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out var rgb))
        {
            return new SKColor(
                (byte)((rgb >> 16) & 0xFF),
                (byte)((rgb >> 8) & 0xFF),
                (byte)(rgb & 0xFF),
                0xFF);
        }

        if (hex.Length == 8 &&
            uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out var argb))
        {
            return new SKColor(
                (byte)((argb >> 16) & 0xFF),
                (byte)((argb >> 8) & 0xFF),
                (byte)(argb & 0xFF),
                (byte)((argb >> 24) & 0xFF));
        }

        return SKColors.Black;
    }

    // ---------------------------------------------------------------------
    // Кодирование в PNG
    // ---------------------------------------------------------------------

    private static byte[] EncodeToPng(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);

        if (data is null)
        {
            throw new InvalidDataException(
                "Не удалось закодировать результат в PNG.");
        }

        return data.ToArray();
    }
}