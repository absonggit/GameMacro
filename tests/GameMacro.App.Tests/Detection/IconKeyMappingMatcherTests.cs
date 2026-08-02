using GameMacro.App.Detection;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Detection;

public sealed class IconKeyMappingMatcherTests
{
    [Fact]
    public void Selects_closest_enabled_mapping_below_threshold()
    {
        var farther = Mapping("F1", [0d, 0d], .5);
        var closest = Mapping("F2", [.2, .2], .5);

        var result = IconKeyMappingMatcher.Match([.18, .18], [farther, closest]);

        Assert.NotNull(result);
        Assert.Same(closest, result.Mapping);
    }

    [Fact]
    public void Returns_null_when_no_mapping_reaches_threshold()
    {
        var mapping = Mapping("F1", [0d, 0d], .1);

        Assert.Null(IconKeyMappingMatcher.Match([1d, 1d], [mapping]));
    }

    [Fact]
    public void Selects_clearly_closest_mapping_when_border_and_scale_raise_absolute_distance()
    {
        var expected = Mapping("F1", [0d], .18);
        var other = Mapping("F2", [2d], .18);

        var result = IconKeyMappingMatcher.Match([.78], [expected, other]);

        Assert.NotNull(result);
        Assert.Same(expected, result.Mapping);
    }

    [Fact]
    public void Ignores_disabled_uncalibrated_and_wrong_length_mappings()
    {
        var disabled = Mapping("F1", [0d], .1);
        disabled.Enabled = false;
        var uncalibrated = Mapping("F2", [0d], .1);
        uncalibrated.Signature = [];
        var wrongLength = Mapping("F3", [0d, 0d], .1);

        Assert.Null(IconKeyMappingMatcher.Match([0d], [disabled, uncalibrated, wrongLength]));
    }

    [Fact]
    public void Matcher_has_no_persistent_icon_or_change_gate()
    {
        var a = Mapping("F1", [0d], .1);
        var b = Mapping("F2", [1d], .1);
        var samples = new[] { new[] { 0d }, new[] { 0d }, new[] { 1d } };

        var keys = samples.Select(sample => IconKeyMappingMatcher.Match(sample, [a, b])!.Mapping.ActionKey);

        Assert.Equal(["F1", "F1", "F2"], keys);
    }

    [Fact]
    public void Current_visual_signature_returns_a_closed_set_match_even_when_two_templates_only_differ_by_position()
    {
        var left = CurrentMapping("F1", Artwork(16));
        var right = CurrentMapping("F2", Artwork(30));
        var between = IconVisualSignature.Create(Artwork(22), 64, 64);

        var result = IconKeyMappingMatcher.Match(between, [left, right]);

        Assert.NotNull(result);
        Assert.Contains(result.Mapping, new[] { left, right });
    }

    [Fact]
    public void Current_visual_signature_does_not_match_an_empty_dynamic_slot()
    {
        var mapping = CurrentMapping("F1", Artwork(16));
        var empty = IconVisualSignature.Create(Solid(18), 64, 64);

        Assert.Null(IconKeyMappingMatcher.Match(empty, [mapping]));
    }

    [Fact]
    public void Color_identity_outweighs_geometry_shift_between_source_bar_and_dynamic_slot()
    {
        var redSource = CurrentMapping("F3", ColoredBar(10, 220, 45, 35));
        var goldSource = CurrentMapping("1", ColoredBar(34, 225, 155, 25));
        var shiftedRedDynamic = IconVisualSignature.Create(ColoredBar(34, 220, 45, 35), 64, 64);

        var result = IconKeyMappingMatcher.Match(shiftedRedDynamic, [redSource, goldSource]);

        Assert.NotNull(result);
        Assert.Same(redSource, result.Mapping);
    }

    [Fact]
    public void Central_shape_outweighs_shared_gold_color_and_top_status_overlay()
    {
        var expected = CurrentMapping("F2", GoldRing());
        var other = CurrentMapping("1", GoldDiagonal());
        var dynamicRing = GoldRing(topOverlay: true, centerY: 42);

        var result = IconKeyMappingMatcher.Match(
            IconTemplateNormalizer.CreateSignature(dynamicRing, 64, 64),
            [expected, other]);

        Assert.NotNull(result);
        Assert.Same(expected, result.Mapping);
    }

    private static IconKeyMapping Mapping(string key, double[] signature, double threshold) => new()
    {
        ActionKey = key,
        Signature = signature,
        PreviewPng = "png",
        MatchThreshold = threshold
    };

    private static IconKeyMapping CurrentMapping(string key, byte[] pixels) => new()
    {
        ActionKey = key,
        Signature = IconVisualSignature.Create(pixels, 64, 64),
        PreviewPng = "png",
        MatchThreshold = .001
    };

    private static byte[] Artwork(int x)
    {
        var pixels = Solid(18);
        for (var y = 8; y < 56; y++)
        for (var px = x; px < x + 18; px++) Set(pixels, px, y, 235, 125, 25);
        return pixels;
    }

    private static byte[] ColoredBar(int x, byte red, byte green, byte blue)
    {
        var pixels = Solid(18);
        for (var y = 8; y < 56; y++)
        for (var px = x; px < x + 18; px++) Set(pixels, px, y, red, green, blue);
        return pixels;
    }

    private static byte[] GoldRing(bool topOverlay = false, int centerY = 34)
    {
        var pixels = Solid(25);
        for (var y = 5; y < 59; y++)
        for (var x = 5; x < 59; x++)
        {
            var distance = Math.Sqrt(Math.Pow(x - 32, 2) + Math.Pow(y - centerY, 2));
            if (distance is > 13 and < 20) Set(pixels, x, y, 240, 175, 35);
        }
        if (!topOverlay) return pixels;
        for (var y = 5; y < 20; y++)
        for (var x = 18; x < 56; x++) Set(pixels, x, y, 35, 32, 28);
        for (var x = 22; x < 52; x += 7)
        for (var y = 8; y < 18; y++) Set(pixels, x, y, 220, 215, 190);
        return pixels;
    }

    private static byte[] GoldDiagonal()
    {
        var pixels = Solid(25);
        for (var y = 7; y < 57; y++)
        for (var thickness = -5; thickness <= 5; thickness++)
        {
            var x = y + thickness;
            if (x is >= 0 and < 64) Set(pixels, x, y, 240, 175, 35);
        }
        return pixels;
    }

    private static byte[] Solid(byte value)
    {
        var pixels = new byte[64 * 64 * 4];
        for (var y = 0; y < 64; y++)
        for (var x = 0; x < 64; x++) Set(pixels, x, y, value, value, value);
        return pixels;
    }

    private static void Set(byte[] pixels, int x, int y, byte red, byte green, byte blue)
    {
        var offset = (y * 64 + x) * 4;
        pixels[offset] = blue;
        pixels[offset + 1] = green;
        pixels[offset + 2] = red;
        pixels[offset + 3] = 255;
    }
}
