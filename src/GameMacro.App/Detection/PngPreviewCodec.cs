using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GameMacro.App.Detection;

public static class PngPreviewCodec
{
    public static string EncodeBgra(byte[] pixels, int width, int height)
    {
        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return Convert.ToBase64String(stream.ToArray());
    }

    public static BitmapSource? Decode(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64)) return null;
        try
        {
            using var stream = new MemoryStream(Convert.FromBase64String(base64));
            var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var bitmap = decoder.Frames[0];
            bitmap.Freeze();
            return bitmap;
        }
        catch (FormatException) { return null; }
        catch (NotSupportedException) { return null; }
    }
}
