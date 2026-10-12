using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using ClosedXML.Excel;

namespace CertificateGenerator.Infrastructure.Readers;

/// <summary>
/// Читает таблицу из Excel-файла (.xlsx) через ClosedXML.
/// Берёт первый лист книги.
/// </summary>
public sealed class ClosedXmlTableReader : ITableReader
{
    /// <inheritdoc />
    public TableData Read(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"Файл не найден: {filePath}",
                filePath);
        }

        using var workbook = new XLWorkbook(filePath);

        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException(
                "В файле нет ни одного листа.");

        var lastColumn = worksheet.LastColumnUsed()
            ?? throw new InvalidDataException(
                "Лист не содержит данных.");

        var lastRow = worksheet.LastRowUsed()
            ?? throw new InvalidDataException(
                "Лист не содержит данных.");

        int columnCount = lastColumn.ColumnNumber();
        int lastRowNumber = lastRow.RowNumber();

        // Первая использованная строка — заголовки.
        int headerRowNumber = worksheet.FirstRowUsed()!.RowNumber();

        var columns = ReadColumns(worksheet, headerRowNumber, columnCount);

        if (columns.All(c => string.IsNullOrWhiteSpace(c.Name)))
        {
            throw new InvalidDataException(
                "Первая строка файла не содержит заголовков колонок.");
        }

        var rows = ReadRows(worksheet, headerRowNumber + 1, lastRowNumber, columnCount);

        // Отбрасываем пустые строки в конце.
        while (rows.Count > 0 && rows[^1].All(string.IsNullOrWhiteSpace))
        {
            rows.RemoveAt(rows.Count - 1);
        }

        return new TableData(columns, rows);
    }

    private static List<ColumnInfo> ReadColumns(
        IXLWorksheet worksheet,
        int headerRowNumber,
        int columnCount)
    {
        var columns = new List<ColumnInfo>(columnCount);

        for (int i = 1; i <= columnCount; i++)
        {
            var cell = worksheet.Cell(headerRowNumber, i);
            string name = cell.GetString();
            columns.Add(new ColumnInfo(i - 1, name));
        }

        return columns;
    }

    private static List<IReadOnlyList<string>> ReadRows(
        IXLWorksheet worksheet,
        int firstDataRowNumber,
        int lastRowNumber,
        int columnCount)
    {
        var rows = new List<IReadOnlyList<string>>();

        for (int rowNumber = firstDataRowNumber;
             rowNumber <= lastRowNumber;
             rowNumber++)
        {
            var cells = new List<string>(columnCount);

            for (int col = 1; col <= columnCount; col++)
            {
                cells.Add(worksheet.Cell(rowNumber, col).GetString());
            }

            rows.Add(cells);
        }

        return rows;
    }
}