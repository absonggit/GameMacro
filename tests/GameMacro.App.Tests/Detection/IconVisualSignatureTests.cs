using GameMacro.App.Detection;

namespace GameMacro.App.Tests.Detection;

public sealed class IconVisualSignatureTests
{
    [Fact]
    public void Distinguishes_equal_brightness_artwork_with_different_colors()
    {
        var amber = Pattern(64, 64, (220, 120, 20));
        var cyan = Pattern(64, 64, (20, 145, 210));

        var amberSignature = IconVisualSignature.Create(amber, 64, 64);
        var cyanSignature = IconVisualSignature.Create(cyan, 64, 64);

        Assert.True(IconVisualSignature.Distance(amberSignature, cyanSignature) > .12);
    }

    [Fact]
    public void Distinguishes_different_edge_shapes()
    {
        var vertical = Solid(64, 64, 20);
        var horizontal = Solid(64, 64, 20);
        PaintRect(vertical, 64, 28, 4, 8, 56, 230, 230, 230);
        PaintRect(horizontal, 64, 4, 28, 56, 8, 230, 230, 230);

        var verticalSignature = IconVisualSignature.Create(vertical, 64, 64);
        var horizontalSignature = IconVisualSignature.Create(horizontal, 64, 64);

        Assert.True(IconVisualSignature.Distance(verticalSignature, horizontalSignature) > .12);
    }

    [Fact]
    public void Small_artwork_shift_remains_closer_than_a_different_shape()
    {
        var original = Solid(64, 64, 20);
        var shifted = Solid(64, 64, 20);
        var different = Solid(64, 64, 20);
        PaintRect(original, 64, 17, 10, 20, 42, 230, 100, 20);
        PaintRect(shifted, 64, 23, 10, 20, 42, 230, 100, 20);
        PaintRect(different, 64, 10, 22, 44, 18, 20, 150, 230);

        var originalSignature = IconVisualSignature.Create(original, 64, 64);
        var shiftedDistance = IconVisualSignature.Distance(originalSignature,
            IconVisualSignature.Create(shifted, 64, 64));
        var differentDistance = IconVisualSignature.Distance(originalSignature,
            IconVisualSignature.Create(different, 64, 64));

        Assert.True(shiftedDistance < differentDistance * .7,
            $"shifted={shiftedDistance:0.000}, different={differentDistance:0.000}");
    }

    private static byte[] Pattern(int width, int height, (byte R, byte G, byte B) color)
    {
        var pixels = Solid(width, height, 15);
        PaintRect(pixels, width, 8, 8, 48, 48, color.R, color.G, color.B);
        PaintRect(pixels, width, 24, 4, 16, 56, 240, 240, 240);
        return pixels;
    }

    private static byte[] Solid(int width, int height, byte value)
    {
        var pixels = new byte[width * height * 4];
        PaintRect(pixels, width, 0, 0, width, height, value, value, value);
        return pixels;
    }

    private static void PaintRect(byte[] pixels, int width, int x, int y, int rectWidth, int rectHeight,
        byte r, byte g, byte b)
    {
        for (var py = y; py < y + rectHeight; py++)
        for (var px = x; px < x + rectWidth; px++)
        {
            var offset = (py * width + px) * 4;
            pixels[offset] = b;
            pixels[offset + 1] = g;
            pixels[offset + 2] = r;
            pixels[offset + 3] = 255;
        }
    }
}
