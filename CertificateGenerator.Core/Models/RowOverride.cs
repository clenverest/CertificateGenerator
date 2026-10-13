namespace CertificateGenerator.Core.Models;

/// <summary>
/// Правки одной строки таблицы, сохранённые в проекте.
/// При загрузке проекта накладываются поверх исходной таблицы.
/// </summary>
public sealed class RowOverride
{
    /// <summary>
    /// Индекс строки в исходной таблице. Начинается с 0.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// Значения всех ячеек строки в порядке колонок.
    /// </summary>
    public IReadOnlyList<string> Values { get; }

    /// <summary>
    /// Создать override строки.
    /// </summary>
    /// <param name="rowIndex">Индекс строки. Не может быть отрицательным.</param>
    /// <param name="values">Значения ячеек.</param>
    /// <exception cref="ArgumentException">
    /// Бросается, если <paramref name="rowIndex"/> отрицательный.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Бросается, если <paramref name="values"/> равен <c>null</c>.
    /// </exception>
    public RowOverride(int rowIndex, IReadOnlyList<string> values)
    {
        if (rowIndex < 0)
        {
            throw new ArgumentException(
                "Индекс строки не может быть отрицательным.",
                nameof(rowIndex));
        }

        ArgumentNullException.ThrowIfNull(values);

        RowIndex = rowIndex;
        Values = values.ToArray();
    }
}