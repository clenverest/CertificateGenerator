namespace CertificateGenerator.Core.Services;

/// <summary>
/// Уникализация имён файлов: если имя занято, добавляет суффикс «_2», «_3» и т.д.
/// Перед расширением: «Иванов.pdf» → «Иванов_2.pdf» → «Иванов_3.pdf».
/// Сравнение регистронезависимое.
/// </summary>
public static class FileNameUniquifier
{
    /// <summary>
    /// Вернуть уникальное имя файла.
    /// </summary>
    /// <param name="desiredName">
    /// Желаемое имя с расширением, например «Иванов Иван.pdf».
    /// </param>
    /// <param name="existingNames">
    /// Уже занятые имена. Сравнение регистронезависимое.
    /// </param>
    /// <returns>
    /// Уникальное имя. Если <paramref name="desiredName"/> свободно —
    /// возвращает его как есть.
    /// </returns>
    public static string MakeUnique(
        string desiredName,
        IEnumerable<string> existingNames)
    {
        if (string.IsNullOrWhiteSpace(desiredName))
        {
            throw new ArgumentException(
                "Желаемое имя файла не может быть пустым.",
                nameof(desiredName));
        }

        if (existingNames is null)
        {
            throw new ArgumentNullException(nameof(existingNames));
        }

        var taken = new HashSet<string>(
            existingNames,
            StringComparer.OrdinalIgnoreCase);

        // Свободно — вернуть как есть.
        if (!taken.Contains(desiredName))
        {
            return desiredName;
        }

        // Разделим на «имя» и «расширение».
        var extension = Path.GetExtension(desiredName);
        var baseName = Path.GetFileNameWithoutExtension(desiredName);

        // Пробуем «имя_2.ext», «имя_3.ext», ... пока не найдём свободное.
        var counter = 2;
        while (true)
        {
            var candidate = $"{baseName}_{counter}{extension}";

            if (!taken.Contains(candidate))
            {
                return candidate;
            }

            counter++;
        }
    }
}