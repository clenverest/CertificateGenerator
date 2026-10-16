using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Tests.Fakes;

/// <summary>
/// Фейковый рендер грамоты: возвращает заранее заданные байты.
/// Запоминает вызовы.
/// </summary>
public sealed class FakeTemplateRenderer : ITemplateRenderer
{
    /// <summary>
    /// Байты, которые вернёт RenderPreview. По умолчанию — пустой массив.
    /// </summary>
    public byte[] BytesToReturn { get; set; } = new byte[] { 1, 2, 3 };

    /// <summary>
    /// Исключение, которое бросит RenderPreview, если задано.
    /// </summary>
    public Exception? ExceptionToThrow { get; set; }

    /// <summary>
    /// Все вызовы: (путь к шаблону, размещения, значения полей).
    /// </summary>
    public List<(string TemplatePath,
                 IReadOnlyList<FieldPlacement> Placements,
                 IReadOnlyDictionary<string, string> FieldValues)> Calls { get; } = new();

    public byte[] RenderPreview(
        string templatePath,
        IReadOnlyList<FieldPlacement> placements,
        IReadOnlyDictionary<string, string> fieldValues)
    {
        Calls.Add((templatePath, placements, fieldValues));

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return BytesToReturn;
    }
}