using GameMacro.App.Detection;

namespace GameMacro.App.Tests.Detection;

public sealed class IconTemplateNormalizerTests
{
    [Fact]
    public void Ignores_outer_border_and_bottom_key_label()
    {
        const int size = 100;
        var first = Solid(size, size, 10, 20, 30);
        var second = Solid(size, size, 180, 160, 140);
        PaintSharedArtwork(first, size, size);
        PaintSharedArtwork(second, size, size);
        PaintRect(first, size, 0, 72, 100, 28, 255, 255, 255);
        PaintRect(second, size, 0, 72, 100, 28, 0, 0, 0);

        var firstSignature = IconTemplateNormalizer.CreateSignature(first, size, size);
        var secondSignature = IconTemplateNormalizer.CreateSignature(second, size, size);

        Assert.True(IconStateClassifier.Distance(firstSignature, secondSignature) < .01);
    }

    [Fact]
    public void Rejects_tiny_images()
    {
        Assert.Throws<ArgumentException>(() => IconTemplateNormalizer.CreateSignature(new byte[4 * 4 * 4], 4, 4));
    }

    private static byte[] Solid(int width, int height, byte r, byte g, byte b)
    {
        var pixels = new byte[width * height * 4];
        PaintRect(pixels, width, 0, 0, width, height, r, g, b);
        return pixels;
    }

    private static void PaintSharedArtwork(byte[] pixels, int width, int height)
    {
        for (var y = 8; y < 72; y++)
        for (var x = 10; x < 90; x++)
        {
            var value = (byte)((x * 3 + y * 5) % 240 + 10);
            SetPixel(pixels, width, x, y, value, (byte)(255 - value), (byte)(value / 2));
        }
    }

    private static void PaintRect(byte[] pixels, int width, int x, int y, int rectWidth, int rectHeight, byte r, byte g, byte b)
    {
        for (var py = y; py < y + rectHeight; py++)
        for (var px = x; px < x + rectWidth; px++) SetPixel(pixels, width, px, py, r, g, b);
    }

    private static void SetPixel(byte[] pixels, int width, int x, int y, byte r, byte g, byte b)
    {
        var offset = (y * width + x) * 4;
        pixels[offset] = b;
        pixels[offset + 1] = g;
        pixels[offset + 2] = r;
        pixels[offset + 3] = 255;
    }
}
