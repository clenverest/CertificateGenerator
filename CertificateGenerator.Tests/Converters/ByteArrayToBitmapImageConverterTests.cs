using System.Globalization;
using System.Windows.Media.Imaging;
using CertificateGenerator.Wpf.Converters;
using SkiaSharp;

namespace CertificateGenerator.Tests.Converters;

public sealed class ByteArrayToBitmapImageConverterTests
{
    private readonly ByteArrayToBitmapImageConverter _converter = new();

    [Fact]
    public void Convert_Null_ReturnsNull()
    {
        var result = _converter.Convert(
            null, typeof(BitmapImage), null, CultureInfo.InvariantCulture);

        Assert.Null(result);
    }

    [Fact]
    public void Convert_EmptyArray_ReturnsNull()
    {
        var result = _converter.Convert(
            Array.Empty<byte>(), typeof(BitmapImage), null, CultureInfo.InvariantCulture);

        Assert.Null(result);
    }

    [Fact]
    public void Convert_NotByteArray_ReturnsNull()
    {
        var result = _converter.Convert(
            "не байты", typeof(BitmapImage), null, CultureInfo.InvariantCulture);

        Assert.Null(result);
    }

    [Fact]
    public void Convert_ValidPngBytes_ReturnsFrozenBitmapImage()
    {
        var pngBytes = CreatePng(64, 32);

        var result = _converter.Convert(
            pngBytes, typeof(BitmapImage), null, CultureInfo.InvariantCulture);

        var image = Assert.IsType<BitmapImage>(result);
        Assert.True(image.IsFrozen);
        Assert.Equal(64, image.PixelWidth);
        Assert.Equal(32, image.PixelHeight);
    }

    [Fact]
    public void ConvertBack_Throws()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(
                null, typeof(byte[]), null, CultureInfo.InvariantCulture));
    }

    // ---------------------------------------------------------------------
    // Помощник: сгенерировать валидный PNG
    // ---------------------------------------------------------------------

    private static byte[] CreatePng(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);

        return data.ToArray();
    }
}