using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace CertificateGenerator.Infrastructure.Generators;

/// <summary>
/// Генерация PDF-грамоты через QuestPDF.
/// Сначала PNG-шаблон с наложенным текстом рендерится Skia,
/// потом полученная картинка вставляется в PDF-страницу
/// ровно того же размера (в пересчёте 300 DPI).
/// </summary>
public sealed class QuestPdfGenerator : IPdfGenerator
{
    /// <summary>
    /// Разрешение, с которым px из PNG переводятся в points PDF.
    /// 300 DPI — стандарт печати.
    /// </summary>
    private const float Dpi = 300f;

    private readonly ITemplateRenderer _templateRenderer;

    public QuestPdfGenerator(ITemplateRenderer templateRenderer)
    {
        _templateRenderer = templateRenderer
            ?? throw new ArgumentNullException(nameof(templateRenderer));
    }

    public byte[] GenerateCertificate(
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

        // 1. Рендерим PNG с наложенным текстом.
        var png = _templateRenderer.RenderPreview(
            templatePath, placements, fieldValues);

        // 2. Узнаём размеры PNG.
        //    SKBitmap.Decode может бросить ArgumentNullException,
        //    если байты — не картинка. Переводим в наш тип ошибки.
        SKBitmap? bitmap;
        try
        {
            bitmap = SKBitmap.Decode(png);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException(
                "Не удалось декодировать PNG-шаблон.", ex);
        }

        if (bitmap is null)
        {
            throw new InvalidDataException(
                "Не удалось декодировать PNG-шаблон.");
        }

        using (bitmap)
        {
            // 3. Переводим пиксели в points (300 DPI).
            var widthPt = bitmap.Width * 72f / Dpi;
            var heightPt = bitmap.Height * 72f / Dpi;

            // 4. Создаём PDF: одна страница размером с картинку,
            //    без отступов, картинка на всю страницу.
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(widthPt, heightPt);
                    page.Margin(0);
                    page.Content().Image(png);
                });
            }).GeneratePdf();
        }
    }
}