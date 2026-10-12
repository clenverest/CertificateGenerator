namespace CertificateGenerator.Core.Models;

/// <summary>
/// Тип поля проекта.
/// </summary>
public enum FieldKind
{
    /// <summary>
    /// Обычное поле: значение берётся из одной колонки таблицы.
    /// </summary>
    Simple = 0,

    /// <summary>
    /// Поле ФИО: значение склеивается из 1–3 колонок таблицы.
    /// </summary>
    FullName = 1
}