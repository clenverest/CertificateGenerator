using System.Globalization;
using System.Windows.Data;

namespace CertificateGenerator.Wpf.Converters;

/// <summary>
/// Инвертирует bool. true → false, false → true.
/// Используется для IsEnabled, когда нужна инверсия.
/// </summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is bool b)
        {
            return !b;
        }

        return false;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is bool b)
        {
            return !b;
        }

        return false;
    }
}