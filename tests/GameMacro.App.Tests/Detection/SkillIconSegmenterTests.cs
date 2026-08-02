using GameMacro.App.Detection;

namespace GameMacro.App.Tests.Detection;

public sealed class SkillIconSegmenterTests
{
    [Fact]
    public void Detects_differently_sized_icons_without_fixed_grid_and_skips_empty_slot()
    {
        var image = Canvas(260, 170);
        DrawIcon(image, 260, 12, 15, 52, 1);
        DrawIcon(image, 260, 92, 8, 64, 2);
        DrawEmptySlot(image, 260, 180, 14, 58);
        DrawIcon(image, 260, 35, 94, 56, 3);
        SetPixel(image, 260, 242, 150, 255, 255, 0);

        var result = SkillIconSegmenter.Segment(image, 260, 170);

        Assert.Equal(3, result.Icons.Count);
        Assert.Equal(2, result.Icons.Count(icon => icon.Region.Y < 80));
        Assert.Single(result.Icons.Where(icon => icon.Region.Y >= 80));
        Assert.True(result.EmptyFilteredCount >= 1);
    }

    [Fact]
    public void Orders_multiple_rows_top_to_bottom_then_left_to_right()
    {
        var image = Canvas(220, 150);
        DrawIcon(image, 220, 110, 12, 48, 2);
        DrawIcon(image, 220, 15, 15, 48, 1);
        DrawIcon(image, 220, 70, 85, 48, 3);

        var icons = SkillIconSegmenter.Segment(image, 220, 150).Icons;

        Assert.Equal(3, icons.Count);
        Assert.True(icons[0].Region.X < icons[1].Region.X);
        Assert.True(icons[0].Region.Y < icons[2].Region.Y);
        Assert.True(icons[1].Region.Y < icons[2].Region.Y);
    }

    [Fact]
    public void Removes_duplicate_artwork()
    {
        var image = Canvas(150, 80);
        DrawIcon(image, 150, 8, 10, 52, 7);
        DrawIcon(image, 150, 85, 10, 52, 7);

        var result = SkillIconSegmenter.Segment(image, 150, 80);

        Assert.Single(result.Icons);
        Assert.Equal(1, result.DuplicateCount);
    }

    [Fact]
    public void Detects_single_row_when_selection_is_tight_vertically()
    {
        var image = Canvas(150, 58);
        DrawIcon(image, 150, 8, 3, 52, 1);
        DrawIcon(image, 150, 85, 3, 52, 2);

        var result = SkillIconSegmenter.Segment(image, 150, 58);

        Assert.Equal(2, result.Icons.Count);
    }

    [Fact]
    public void Ignores_small_textured_component_contained_inside_a_full_icon_slot()
    {
        var image = Canvas(300, 120);
        DrawEmptySlot(image, 300, 10, 8, 104);
        DrawIcon(image, 300, 48, 48, 22, 5);

        var result = SkillIconSegmenter.Segment(image, 300, 120);

        Assert.Single(result.Icons);
        Assert.True(result.Icons[0].Region.Width >= 90);
    }

    private static byte[] Canvas(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++) SetPixel(pixels, width, x, y, 28, 38, 48);
        return pixels;
    }

    private static void DrawIcon(byte[] pixels, int canvasWidth, int x, int y, int size, int seed)
    {
        DrawRect(pixels, canvasWidth, x, y, size, size, 82, 91, 104);
        DrawRect(pixels, canvasWidth, x + 3, y + 3, size - 6, size - 6, 20, 25, 30);
        for (var py = y + 5; py < y + size - 5; py++)
        for (var px = x + 5; px < x + size - 5; px++)
        {
            var value = (byte)((((px - x) * (seed + 2) + (py - y) * (seed + 5) + seed * 31) % 210) + 35);
            SetPixel(pixels, canvasWidth, px, py, value, (byte)((value + seed * 43) % 255), (byte)(255 - value));
        }
    }

    private static void DrawEmptySlot(byte[] pixels, int canvasWidth, int x, int y, int size)
    {
        DrawRect(pixels, canvasWidth, x, y, size, size, 75, 84, 96);
        DrawRect(pixels, canvasWidth, x + 3, y + 3, size - 6, size - 6, 31, 41, 51);
    }

    private static void DrawRect(byte[] pixels, int width, int x, int y, int rectWidth, int rectHeight, byte r, byte g, byte b)
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
