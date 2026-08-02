using GameMacro.App.Detection;

namespace GameMacro.App.Tests.Detection;

public sealed class PixelIconTemplateBuilderTests
{
    [Fact]
    public void Creates_a_fixed_size_color_template_from_different_input_sizes()
    {
        var small = PixelIconTemplateBuilder.Create(TestIconFrames.Ring(64, 64), 64, 64);
        var large = PixelIconTemplateBuilder.Create(TestIconFrames.Ring(108, 108), 108, 108);

        Assert.Equal(PixelIconTemplate.Size * PixelIconTemplate.Size * 3, small.Rgb.Length);
        Assert.Equal(small.Rgb.Length, large.Rgb.Length);
        Assert.Equal(PixelIconTemplate.CurrentVersion, small.Version);
    }

    [Fact]
    public void Serialized_template_round_trips_without_losing_pixels()
    {
        var template = PixelIconTemplateBuilder.Create(TestIconFrames.Ring(64, 64), 64, 64);

        var restored = PixelIconTemplate.Deserialize(template.Serialize());

        Assert.NotNull(restored);
        Assert.Equal(template.Rgb, restored.Rgb);
    }
}

internal static class TestIconFrames
{
    public static byte[] Ring(int width, int height, bool overlay = false, int shiftY = 0)
    {
        var pixels = Solid(width, height, 22);
        var centerX = width / 2;
        var centerY = height / 2 + shiftY;
        var outer = Math.Min(width, height) * .29;
        var inner = Math.Min(width, height) * .18;
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var radius = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
            if (radius > inner && radius < outer) Set(pixels, width, x, y, 240, 174, 35);
        }
        if (overlay)
        {
            for (var y = height / 8; y < height / 3; y++)
            for (var x = width / 4; x < width * 3 / 4; x++) Set(pixels, width, x, y, 30, 28, 25);
            for (var x = width / 3; x < width * 2 / 3; x += Math.Max(2, width / 12))
            for (var y = height / 6; y < height / 3; y++) Set(pixels, width, x, y, 215, 210, 185);
        }
        return pixels;
    }

    public static byte[] Diagonal(int width, int height)
    {
        var pixels = Solid(width, height, 22);
        for (var y = height / 8; y < height * 7 / 8; y++)
        for (var thickness = -Math.Max(2, width / 14); thickness <= Math.Max(2, width / 14); thickness++)
        {
            var x = y * width / height + thickness;
            if (x >= 0 && x < width) Set(pixels, width, x, y, 240, 174, 35);
        }
        return pixels;
    }

    public static byte[] Cross(int width, int height)
    {
        var pixels = Solid(width, height, 22);
        for (var y = height / 5; y < height * 4 / 5; y++)
        for (var x = width / 2 - 3; x <= width / 2 + 3; x++) Set(pixels, width, x, y, 85, 145, 235);
        for (var x = width / 5; x < width * 4 / 5; x++)
        for (var y = height / 2 - 3; y <= height / 2 + 3; y++) Set(pixels, width, x, y, 85, 145, 235);
        return pixels;
    }

    public static byte[] ParallelStreaks(int width, int height)
    {
        var pixels = Solid(width, height, 24);
        var thickness = Math.Max(1, width / 45);
        foreach (var offset in new[] { -.18, 0d, .18 })
        for (var y = height / 7; y < height * 6 / 7; y++)
        for (var dx = -thickness; dx <= thickness; dx++)
        {
            var x = (int)Math.Round(width * (.22 + offset) + y * .62) + dx;
            if (x >= 0 && x < width) Set(pixels, width, x, y, 238, 72, 45);
        }
        return pixels;
    }

    public static byte[] CurvedSweep(int width, int height, bool darkTopRight = false)
    {
        var pixels = Solid(width, height, 24);
        var thickness = Math.Max(2, width / 22);
        for (var y = height / 7; y < height * 6 / 7; y++)
        for (var dx = -thickness; dx <= thickness; dx++)
        {
            var normalizedY = y / (double)height - .5;
            var x = (int)Math.Round(width * (.47 + normalizedY * .54 + normalizedY * normalizedY * .62)) + dx;
            if (x >= 0 && x < width) Set(pixels, width, x, y, 238, 78, 42);
        }
        return darkTopRight ? AddTopRightGradient(pixels, width, height) : pixels;
    }

    public static byte[] AddTopRightGradient(byte[] source, int width, int height)
    {
        var pixels = source.ToArray();
        var startX = width * 3 / 5;
        var endY = height / 2;
        for (var y = 0; y < endY; y++)
        for (var x = startX; x < width; x++)
        {
            var horizontal = (x - startX) / (double)Math.Max(1, width - startX - 1);
            var vertical = 1 - y / (double)Math.Max(1, endY - 1);
            var darkness = Math.Clamp(.36 + horizontal * .52 + vertical * .12, 0, .96);
            var factor = 1 - darkness;
            var index = (y * width + x) * 4;
            pixels[index] = (byte)Math.Round(pixels[index] * factor);
            pixels[index + 1] = (byte)Math.Round(pixels[index + 1] * factor);
            pixels[index + 2] = (byte)Math.Round(pixels[index + 2] * factor);
        }
        return pixels;
    }

    private static byte[] Solid(int width, int height, byte value)
    {
        var pixels = new byte[width * height * 4];
        for (var index = 0; index < pixels.Length; index += 4)
        {
            pixels[index] = value;
            pixels[index + 1] = value;
            pixels[index + 2] = value;
            pixels[index + 3] = 255;
        }
        return pixels;
    }

    private static void Set(byte[] pixels, int width, int x, int y, byte red, byte green, byte blue)
    {
        var offset = (y * width + x) * 4;
        pixels[offset] = blue;
        pixels[offset + 1] = green;
        pixels[offset + 2] = red;
        pixels[offset + 3] = 255;
    }
}
