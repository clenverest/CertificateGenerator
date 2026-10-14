using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace CertificateGenerator.Wpf.Converters;

/// <summary>
/// Конвертер byte[] (PNG-байты) → BitmapImage для отображения в Image.
/// Возвращает null, если байты пустые или null.
/// </summary>
public sealed class ByteArrayToBitmapImageConverter : IValueConverter
{
    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not byte[] bytes || bytes.Length == 0)
        {
            return null;
        }

        var image = new BitmapImage();

        using (var stream = new MemoryStream(bytes))
        {
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
        }

        // Freeze — чтобы BitmapImage можно было использовать
        // из другого потока и не держать ссылку на MemoryStream.
        image.Freeze();

        return image;
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException(
            "Обратное преобразование BitmapImage → byte[] не поддерживается.");
    }
}