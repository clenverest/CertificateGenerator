namespace CertificateGenerator.Core.Models;

/// <summary>
/// Выравнивание текста поля относительно его точки (X, Y) на шаблоне.
/// </summary>
public enum FieldAlignment
{
    /// <summary>Текст начинается от точки X и идёт вправо.</summary>
    Left,

    /// <summary>Точка X — центр текста.</summary>
    Center,

    /// <summary>Текст заканчивается в точке X.</summary>
    Right
}