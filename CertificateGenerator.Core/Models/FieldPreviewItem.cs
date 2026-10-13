namespace CertificateGenerator.Core.Models;

/// <summary>
/// Готовая строка предпросмотра: имя поля и его значение
/// для первого участника таблицы.
/// </summary>
public sealed class FieldPreviewItem
{
    public string FieldName { get; }
    public string PreviewValue { get; }

    public FieldPreviewItem(string fieldName, string previewValue)
    {
        FieldName = fieldName ?? string.Empty;
        PreviewValue = previewValue ?? string.Empty;
    }
}