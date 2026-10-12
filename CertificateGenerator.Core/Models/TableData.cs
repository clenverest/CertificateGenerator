namespace CertificateGenerator.Core.Models;

/// <summary>
/// Таблица с колонками и строками данных.
/// Не зависит от формата источника (Excel, CSV и т.п.).
/// </summary>
public sealed class TableData
{
    public IReadOnlyList<ColumnInfo> Columns { get; }

    public IReadOnlyList<IReadOnlyList<string>> Rows { get; }

    public int RowCount => Rows.Count;

    public int ColumnCount => Columns.Count;

    public TableData(
        IReadOnlyList<ColumnInfo> columns,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        if (columns.Count == 0)
        {
            throw new ArgumentException(
                "Таблица должна содержать хотя бы одну колонку.",
                nameof(columns));
        }

        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Count != columns.Count)
            {
                throw new ArgumentException(
                    $"Строка {i} содержит {rows[i].Count} ячеек, " +
                    $"ожидалось {columns.Count}.",
                    nameof(rows));
            }
        }

        Columns = columns;
        Rows = rows;
    }
}