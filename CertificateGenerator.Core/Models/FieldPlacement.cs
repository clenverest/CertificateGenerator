namespace CertificateGenerator.Core.Models;

/// <summary>
/// Настройка размещения одного поля на шаблоне.
/// Ссылается на поле по имени (<see cref="Field.Name"/>).
/// </summary>
public sealed class FieldPlacement
{
    /// <summary>Имя поля, к которому относится размещение.</summary>
    public string FieldName { get; }

    /// <summary>Координата X в пикселях от левого края шаблона.</summary>
    public int X { get; }

    /// <summary>Координата Y в пикселях от верхнего края шаблона.</summary>
    public int Y { get; }

    /// <summary>Размер шрифта в пикселях.</summary>
    public int FontSize { get; }

    /// <summary>Имя семейства шрифта (например, "Arial").</summary>
    public string FontFamily { get; }

    /// <summary>Выравнивание текста относительно точки (X, Y).</summary>
    public FieldAlignment Alignment { get; }

    /// <summary>Цвет текста в формате "#RRGGBB". Пока всегда чёрный.</summary>
    public string TextColor { get; }

    /// <summary>
    /// Создать размещение поля.
    /// </summary>
    /// <param name="fieldName">Имя поля, к которому относится размещение.</param>
    /// <param name="x">Координата X в пикселях.</param>
    /// <param name="y">Координата Y в пикселях.</param>
    /// <param name="fontSize">Размер шрифта в пикселях, больше нуля.</param>
    /// <param name="fontFamily">Имя шрифта (например, "Arial").</param>
    /// <param name="alignment">Выравнивание текста.</param>
    /// <param name="textColor">Цвет текста, по умолчанию чёрный.</param>
    public FieldPlacement(
        string fieldName,
        int x,
        int y,
        int fontSize,
        string fontFamily,
        FieldAlignment alignment,
        string textColor = "#000000")
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException(
                "Имя поля не может быть пустым.",
                nameof(fieldName));
        }

        if (fontSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fontSize),
                "Размер шрифта должен быть больше нуля.");
        }

        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            throw new ArgumentException(
                "Имя шрифта не может быть пустым.",
                nameof(fontFamily));
        }

        if (string.IsNullOrWhiteSpace(textColor))
        {
            throw new ArgumentException(
                "Цвет текста не может быть пустым.",
                nameof(textColor));
        }

        FieldName = fieldName;
        X = x;
        Y = y;
        FontSize = fontSize;
        FontFamily = fontFamily;
        Alignment = alignment;
        TextColor = textColor;
    }

    // ---------------------------------------------------------------------
    // Иммутабельные обновления: возвращают новый экземпляр.
    // ---------------------------------------------------------------------

    /// <summary>Вернуть копию с новой координатой X.</summary>
    public FieldPlacement WithX(int x)
        => new(FieldName, x, Y, FontSize, FontFamily, Alignment, TextColor);

    /// <summary>Вернуть копию с новой координатой Y.</summary>
    public FieldPlacement WithY(int y)
        => new(FieldName, X, y, FontSize, FontFamily, Alignment, TextColor);

    /// <summary>Вернуть копию с новыми координатами X и Y.</summary>
    public FieldPlacement WithPosition(int x, int y)
        => new(FieldName, x, y, FontSize, FontFamily, Alignment, TextColor);

    /// <summary>Вернуть копию с новым размером шрифта.</summary>
    public FieldPlacement WithFontSize(int fontSize)
        => new(FieldName, X, Y, fontSize, FontFamily, Alignment, TextColor);

    /// <summary>Вернуть копию с новым шрифтом.</summary>
    public FieldPlacement WithFontFamily(string fontFamily)
        => new(FieldName, X, Y, FontSize, fontFamily, Alignment, TextColor);

    /// <summary>Вернуть копию с новым выравниванием.</summary>
    public FieldPlacement WithAlignment(FieldAlignment alignment)
        => new(FieldName, X, Y, FontSize, FontFamily, alignment, TextColor);

    /// <summary>Вернуть копию с новым цветом текста.</summary>
    public FieldPlacement WithTextColor(string textColor)
        => new(FieldName, X, Y, FontSize, FontFamily, Alignment, textColor);
}