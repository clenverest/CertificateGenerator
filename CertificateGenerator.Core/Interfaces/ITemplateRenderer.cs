using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Interfaces;

/// <summary>
/// Рендер грамоты: наложение значений полей на PNG-шаблон.
/// На этом этапе — только превью. PDF — на Этапе 4.
/// </summary>
public interface ITemplateRenderer
{
    /// <summary>
    /// Отрисовать превью грамоты и вернуть PNG-байты.
    /// </summary>
    /// <param name="templatePath">Полный путь к PNG-шаблону.</param>
    /// <param name="placements">Настройки размещения полей.</param>
    /// <param name="fieldValues">
    /// Значения полей: имя поля → текст. Поля без значения
    /// (или без записи в словаре) просто не рисуются.
    /// </param>
    /// <returns>PNG-байты готового превью.</returns>
    byte[] RenderPreview(
        string templatePath,
        IReadOnlyList<FieldPlacement> placements,
        IReadOnlyDictionary<string, string> fieldValues);
}