using GameMacro.App.Detection;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Detection;

public sealed class PixelIconTemplateMatcherTests
{
    [Fact]
    public void Distinguishes_same_color_ring_from_diagonal_after_overlay_and_shift()
    {
        var ring = Candidate("F2", TestIconFrames.Ring(108, 108));
        var diagonal = Candidate("1", TestIconFrames.Diagonal(108, 108));
        var sample = PixelIconTemplateBuilder.Create(TestIconFrames.Ring(76, 80, overlay: true, shiftY: 5), 76, 80);

        var result = PixelIconTemplateMatcher.Match(sample, [ring, diagonal]);

        Assert.NotNull(result);
        Assert.Equal("F2", result.Mapping.ActionKey);
    }

    [Fact]
    public void Rejects_an_unknown_nonempty_icon()
    {
        var ring = Candidate("F2", TestIconFrames.Ring(108, 108));
        var diagonal = Candidate("1", TestIconFrames.Diagonal(108, 108));
        var unknown = PixelIconTemplateBuilder.Create(TestIconFrames.Cross(76, 80), 76, 80);

        Assert.Null(PixelIconTemplateMatcher.Match(unknown, [ring, diagonal]));
    }

    [Fact]
    public void Rejects_a_result_that_is_not_clearly_better_than_the_runner_up()
    {
        var first = Candidate("F1", TestIconFrames.Ring(108, 108));
        var second = Candidate("F2", TestIconFrames.Ring(108, 108, shiftY: 1));
        var sample = PixelIconTemplateBuilder.Create(TestIconFrames.Ring(76, 80), 76, 80);

        Assert.Null(PixelIconTemplateMatcher.Match(sample, [first, second]));
    }

    [Fact]
    public void Top_right_dynamic_gradient_does_not_confuse_parallel_streaks_with_curved_sweep()
    {
        var f3 = Candidate("F3", TestIconFrames.ParallelStreaks(108, 108));
        var tilde = Candidate("~", TestIconFrames.CurvedSweep(108, 108, darkTopRight: true));
        var samplePixels = TestIconFrames.AddTopRightGradient(TestIconFrames.ParallelStreaks(108, 108), 108, 108);
        var sample = PixelIconTemplateBuilder.Create(samplePixels, 108, 108);

        var result = PixelIconTemplateMatcher.Match(sample, [f3, tilde]);

        Assert.InRange(PixelIconTemplateMatcher.Distance(sample, f3.Template), 0, .07);
        Assert.NotNull(result);
        Assert.Equal("F3", result.Mapping.ActionKey);
    }

    [Fact]
    public void Top_right_dynamic_gradient_keeps_curved_sweep_mapped_to_tilde()
    {
        var f3 = Candidate("F3", TestIconFrames.ParallelStreaks(108, 108));
        var tilde = Candidate("~", TestIconFrames.CurvedSweep(108, 108, darkTopRight: true));
        var samplePixels = TestIconFrames.AddTopRightGradient(TestIconFrames.CurvedSweep(108, 108), 108, 108);
        var sample = PixelIconTemplateBuilder.Create(samplePixels, 108, 108);

        var result = PixelIconTemplateMatcher.Match(sample, [f3, tilde]);

        Assert.NotNull(result);
        Assert.Equal("~", result.Mapping.ActionKey);
    }

    private static PixelIconCandidate Candidate(string key, byte[] pixels)
    {
        var mapping = new IconKeyMapping { ActionKey = key, Enabled = true, PreviewPng = "png", MatchThreshold = .18 };
        return new(mapping, PixelIconTemplateBuilder.Create(pixels, 108, 108));
    }
}
