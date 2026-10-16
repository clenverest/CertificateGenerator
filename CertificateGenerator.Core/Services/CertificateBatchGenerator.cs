using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Services;

/// <summary>
/// Массовая генерация грамот: обходит строки таблицы,
/// для каждой генерирует PDF, упаковывает в ZIP.
/// Чистая логика — без UI.
/// </summary>
public sealed class CertificateBatchGenerator
{
    private readonly IPdfGenerator _pdfGenerator;
    private readonly IZipPacker _zipPacker;

    public CertificateBatchGenerator(
        IPdfGenerator pdfGenerator,
        IZipPacker zipPacker)
    {
        _pdfGenerator = pdfGenerator
            ?? throw new ArgumentNullException(nameof(pdfGenerator));
        _zipPacker = zipPacker
            ?? throw new ArgumentNullException(nameof(zipPacker));
    }

    /// <summary>
    /// Сгенерировать грамоты для всех строк таблицы и упаковать в ZIP.
    /// </summary>
    /// <param name="templatePath">Путь к PNG-шаблону.</param>
    /// <param name="placements">Размещения полей.</param>
    /// <param name="fields">Поля проекта.</param>
    /// <param name="rows">Строки таблицы (значения ячеек).</param>
    /// <param name="zipPath">Куда сохранить ZIP.</param>
    /// <param name="progress">Прогресс. Может быть null.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public BatchReport GenerateAll(
        string templatePath,
        IReadOnlyList<FieldPlacement> placements,
        IReadOnlyList<Field> fields,
        IReadOnlyList<IReadOnlyList<string>> rows,
        string zipPath,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templatePath))
        {
            throw new ArgumentException("Путь к шаблону не может быть пустым.", nameof(templatePath));
        }
        if (placements is null)
        {
            throw new ArgumentNullException(nameof(placements));
        }
        if (fields is null)
        {
            throw new ArgumentNullException(nameof(fields));
        }
        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }
        if (string.IsNullOrWhiteSpace(zipPath))
        {
            throw new ArgumentException("Путь к ZIP не может быть пустым.", nameof(zipPath));
        }

        var items = new List<BatchItemResult>(rows.Count);
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        var nameField = GetNameFieldForFileName(fields);

        var wasCancelled = false;

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                wasCancelled = true;
                break;
            }

            var row = rows[rowIndex];
            var displayName = FieldValueResolver.Resolve(nameField, row);

            // Сообщаем прогресс ДО обработки — пользователь видит, кого сейчас.
            progress?.Report(new BatchProgress(
                rowIndex,
                rows.Count,
                string.IsNullOrWhiteSpace(displayName) ? $"(строка {rowIndex + 1})" : displayName));

            // Если имени нет — пропускаем.
            if (string.IsNullOrWhiteSpace(displayName))
            {
                items.Add(new BatchItemResult(
                    rowIndex,
                    $"(строка {rowIndex + 1})",
                    BatchItemStatus.Skipped,
                    message: "Пустое имя — не из чего строить имя файла."));
                continue;
            }

            // Собираем значения всех полей для этой строки.
            var fieldValues = new Dictionary<string, string>();
            foreach (var field in fields)
            {
                fieldValues[field.Name] = FieldValueResolver.Resolve(field, row);
            }

            // Генерируем PDF.
            byte[] pdf;
            try
            {
                pdf = _pdfGenerator.GenerateCertificate(
                    templatePath, placements, fieldValues);
            }
            catch (Exception ex)
            {
                items.Add(new BatchItemResult(
                    rowIndex,
                    displayName,
                    BatchItemStatus.Failed,
                    message: ex.Message));
                continue;
            }

            // Уникализируем имя.
            var desiredName = MakeSafeFileName(displayName) + ".pdf";
            var uniqueName = FileNameUniquifier.MakeUnique(desiredName, files.Keys);

            files[uniqueName] = pdf;

            items.Add(new BatchItemResult(
                rowIndex,
                displayName,
                BatchItemStatus.Success,
                fileName: uniqueName));
        }

        // Финальный прогресс — 100%.
        if (!wasCancelled)
        {
            progress?.Report(new BatchProgress(rows.Count, rows.Count, string.Empty));
        }

        // Пишем ZIP, если есть что писать.
        string? savedZipPath = null;
        if (files.Count > 0)
        {
            _zipPacker.PackToFile(zipPath, files);
            savedZipPath = zipPath;
        }

        return new BatchReport(items, savedZipPath, wasCancelled);
    }

    // ---------------------------------------------------------------------
    // Вспомогательные
    // ---------------------------------------------------------------------

    private static Field GetNameFieldForFileName(IReadOnlyList<Field> fields)
    {
        if (fields.Count == 0)
        {
            throw new InvalidOperationException(
                "В проекте нет полей — не из чего строить имя файла.");
        }

        var fullName = fields.FirstOrDefault(f => f.Kind == FieldKind.FullName);
        return fullName ?? fields[0];
    }

    private static string MakeSafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder(value.Length);

        foreach (var ch in value)
        {
            sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
        }

        return sb.ToString().Trim();
    }
}