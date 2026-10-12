using System.Globalization;
using System.Text;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace CertificateGenerator.Infrastructure.Readers;

/// <summary>
/// Читает таблицу из CSV-файла через CsvHelper.
/// Поддерживает разделители <c>,</c> и <c>;</c>, кодировки UTF-8 (с/без BOM),
/// UTF-16 LE/BE, Windows-1251.
/// </summary>
public sealed class CsvTableReader : ITableReader
{
    /// <inheritdoc />
    
    static CsvTableReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public TableData Read(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"Файл не найден: {filePath}",
                filePath);
        }

        var encoding = DetectEncoding(filePath);
        string text = File.ReadAllText(filePath, encoding);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidDataException("Файл пуст.");
        }

        string firstLine = GetFirstNonEmptyLine(text);
        if (firstLine.Length == 0)
        {
            throw new InvalidDataException("Файл не содержит данных.");
        }

        string delimiter = DetectDelimiter(firstLine);
        var records = ReadRecords(text, delimiter);

        if (records.Count == 0)
        {
            throw new InvalidDataException("Файл не содержит данных.");
        }

        var headerRecord = records[0];
        if (headerRecord.Length == 0 ||
            headerRecord.All(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException(
                "Первая строка файла не содержит заголовков колонок.");
        }

        int columnCount = headerRecord.Length;
        var columns = new List<ColumnInfo>(columnCount);
        for (int i = 0; i < columnCount; i++)
        {
            columns.Add(new ColumnInfo(i, headerRecord[i]));
        }

        var rows = new List<IReadOnlyList<string>>();
        for (int i = 1; i < records.Count; i++)
        {
            rows.Add(records[i]);
        }

        while (rows.Count > 0 &&
               rows[^1].All(string.IsNullOrWhiteSpace))
        {
            rows.RemoveAt(rows.Count - 1);
        }

        return new TableData(columns, rows);
    }

    private static Encoding DetectEncoding(string filePath)
    {
        byte[] bytes = File.ReadAllBytes(filePath);

        if (bytes.Length >= 3 &&
            bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode;
        }

        // Нет BOM — пробуем UTF-8.
        string utf8Text = Encoding.UTF8.GetString(bytes);
        if (utf8Text.Contains('\uFFFD'))
        {
            // Признак неправильной кодировки — пробуем Windows-1251.
            return Encoding.GetEncoding(1251);
        }

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }

    private static string GetFirstNonEmptyLine(string text)
    {
        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                return line;
            }
        }

        return string.Empty;
    }

    private static string DetectDelimiter(string firstLine)
    {
        int commas = 0;
        int semicolons = 0;

        foreach (char c in firstLine)
        {
            if (c == ',') commas++;
            else if (c == ';') semicolons++;
        }

        return semicolons > commas ? ";" : ",";
    }

    private static List<string[]> ReadRecords(string text, string delimiter)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter,
            HasHeaderRecord = false,
            IgnoreBlankLines = false,
            TrimOptions = TrimOptions.Trim,
        };

        var records = new List<string[]>();

        using var textReader = new StringReader(text);
        using var csv = new CsvReader(textReader, config);

        while (csv.Read())
        {
            int count = csv.Parser.Count;
            var fields = new string[count];
            for (int i = 0; i < count; i++)
            {
                fields[i] = csv.GetField(i) ?? string.Empty;
            }
            records.Add(fields);
        }

        return records;
    }
}