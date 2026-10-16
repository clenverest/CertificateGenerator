using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Interfaces;

/// <summary>
/// Генерация PDF-грамоты: PNG-шаблон + размещения полей + значения.
/// </summary>
public interface IPdfGenerator
{
    /// <summary>
    /// Сгенерировать PDF-грамоту.
    /// </summary>
    /// <param name="templatePath">Полный путь к PNG-шаблону.</param>
    /// <param name="placements">Настройки размещения полей.</param>
    /// <param name="fieldValues">
    /// Значения полей: имя поля → текст.
    /// Поля без значения (или без записи в словаре) не рисуются.
    /// </param>
    /// <returns>Байты готового PDF.</returns>
    byte[] GenerateCertificate(
        string templatePath,
        IReadOnlyList<FieldPlacement> placements,
        IReadOnlyDictionary<string, string> fieldValues);
}