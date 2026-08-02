using GameMacro.App.Detection;

namespace GameMacro.App.Tests.Detection;

public sealed class IconStateClassifierTests
{
    [Fact]
    public void Classifies_sample_nearest_to_ready_reference()
    {
        Assert.Equal(IconVisualState.Ready,
            IconStateClassifier.Classify([0.9, 0.1], [1, 0], [0, 1]));
    }

    [Fact]
    public void Returns_unknown_when_distances_are_ambiguous()
    {
        Assert.Equal(IconVisualState.Unknown,
            IconStateClassifier.Classify([0.5, 0.5], [1, 0], [0, 1]));
    }

    [Fact]
    public void Distinguishes_uniform_brightness_change_caused_by_cooldown_overlay()
    {
        var ready = IconStateClassifier.CreateSignature(SolidPixels(220), 8, 8);
        var cooldown = IconStateClassifier.CreateSignature(SolidPixels(70), 8, 8);

        Assert.Equal(IconVisualState.Cooldown, IconStateClassifier.Classify(cooldown, ready, cooldown));
    }

    [Theory]
    [InlineData(0.05, 1)]
    [InlineData(0.14, 0)]
    [InlineData(0.70, 2)]
    public void Single_baseline_uses_hysteresis(double distance, int expectedState)
    {
        var state = IconStateClassifier.Classify([distance], [0], .1, .4);

        Assert.Equal((IconVisualState)expectedState, state);
    }

    [Fact]
    public void Single_baseline_detects_uniform_dark_cooldown_overlay()
    {
        var ready = IconStateClassifier.CreateSignature(SolidPixels(220), 8, 8);
        var dark = IconStateClassifier.CreateSignature(SolidPixels(70), 8, 8);

        Assert.Equal(IconVisualState.Cooldown, IconStateClassifier.Classify(dark, ready, .12, .35));
    }

    [Fact]
    public void Excessive_saved_thresholds_do_not_hide_meaningful_cooldown_change()
    {
        Assert.Equal(IconVisualState.Cooldown,
            IconStateClassifier.Classify([.30], [0], .45, .70));
    }

    private static byte[] SolidPixels(byte value)
    {
        var pixels = new byte[8 * 8 * 4];
        for (var index = 0; index < pixels.Length; index += 4)
            pixels[index] = pixels[index + 1] = pixels[index + 2] = value;
        return pixels;
    }
}
