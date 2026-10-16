using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Tests.Fakes;

/// <summary>
/// Заглушка IPdfGenerator для тестов ViewModel.
/// Не создаёт настоящий PDF — просто запоминает аргументы
/// и возвращает заранее заданные байты.
/// </summary>
public sealed class FakePdfGenerator : IPdfGenerator
{
    /// <summary>Что вернуть из GenerateCertificate.</summary>
    /// <remarks>
    /// По умолчанию — сигнатура PDF-файла: %PDF (4 байта).
    /// </remarks>
    public byte[] Result { get; set; } = new byte[] { 0x25, 0x50, 0x44, 0x46 };

    /// <summary>Путь к шаблону из последнего вызова.</summary>
    public string? LastTemplatePath { get; private set; }

    /// <summary>Размещения из последнего вызова.</summary>
    public IReadOnlyList<FieldPlacement>? LastPlacements { get; private set; }

    /// <summary>Значения полей из последнего вызова.</summary>
    public IReadOnlyDictionary<string, string>? LastFieldValues { get; private set; }

    /// <summary>Сколько раз вызывали GenerateCertificate.</summary>
    public int CallCount { get; private set; }

    /// <summary>
    /// Если задано — GenerateCertificate бросит это исключение.
    /// Установи в тесте, чтобы проверить обработку ошибок.
    /// </summary>
    public Exception? ThrowOnGenerate { get; set; }

    public byte[] GenerateCertificate(
        string templatePath,
        IReadOnlyList<FieldPlacement> placements,
        IReadOnlyDictionary<string, string> fieldValues)
    {
        CallCount++;
        LastTemplatePath = templatePath;
        LastPlacements = placements;
        LastFieldValues = fieldValues;

        if (ThrowOnGenerate is not null)
        {
            throw ThrowOnGenerate;
        }

        return Result;
    }
}