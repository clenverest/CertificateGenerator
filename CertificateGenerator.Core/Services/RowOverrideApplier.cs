using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Core.Services;

/// <summary>
/// Применяет правки строк (<see cref="RowOverride"/>) к таблице.
/// Возвращает новую таблицу — исходная не изменяется.
/// </summary>
public static class RowOverrideApplier
{
    /// <summary>
    /// Применить правки к таблице.
    /// </summary>
    /// <param name="source">Исходная таблица.</param>
    /// <param name="overrides">Правки строк. Пустой список — вернуть
    /// <paramref name="source"/> без изменений.</param>
    /// <returns>
    /// Новая таблица с применёнными правками,
    /// либо <paramref name="source"/>, если правок нет.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Бросается, если <paramref name="source"/> или
    /// <paramref name="overrides"/> равны <c>null</c>.
    /// </exception>
    public static TableData Apply(
        TableData source,
        IReadOnlyList<RowOverride> overrides)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(overrides);

        if (overrides.Count == 0)
        {
            return source;
        }

        // Словарь: RowIndex → Values. Если два override с одинаковым индексом —
        // последний выигрывает (перезаписывает).
        var overrideMap = new Dictionary<int, IReadOnlyList<string>>();
        foreach (var rowOverride in overrides)
        {
            overrideMap[rowOverride.RowIndex] = rowOverride.Values;
        }

        var newRows = new List<IReadOnlyList<string>>(source.Rows.Count);

        for (var i = 0; i < source.Rows.Count; i++)
        {
            if (overrideMap.TryGetValue(i, out var values))
            {
                newRows.Add(NormalizeRowLength(values, source.Columns.Count));
            }
            else
            {
                newRows.Add(source.Rows[i]);
            }
        }

        return new TableData(source.Columns, newRows);
    }

    // ---------------------------------------------------------------------
    // Приватное
    // ---------------------------------------------------------------------

    /// <summary>
    /// Привести строку к нужному количеству ячеек:
    /// дополнить пустыми или обрезать.
    /// </summary>
    private static IReadOnlyList<string> NormalizeRowLength(
        IReadOnlyList<string> values,
        int targetCount)
    {
        if (values.Count == targetCount)
        {
            return values;
        }

        var result = new List<string>(targetCount);

        for (var i = 0; i < targetCount; i++)
        {
            result.Add(i < values.Count ? values[i] : string.Empty);
        }

        return result;
    }
}