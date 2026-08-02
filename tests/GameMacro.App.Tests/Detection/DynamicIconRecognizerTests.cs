using GameMacro.App.Detection;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Detection;

public sealed class DynamicIconRecognizerTests
{
    [Fact]
    public void Matches_every_frame_so_fast_transitions_cannot_reuse_an_empty_result()
    {
        var calls = 0;
        var mapping = Mapping("F1", [0d, 0d]);
        var recognizer = new DynamicIconRecognizer((_, _) =>
        {
            calls++;
            return new(mapping, 0);
        });

        recognizer.Match([0d, 0d], [mapping]);
        recognizer.Match([.01, .01], [mapping]);
        recognizer.Match([1d, 1d], [mapping]);

        Assert.Equal(3, calls);
    }

    [Fact]
    public void Reset_forces_next_frame_to_match_again()
    {
        var calls = 0;
        var mapping = Mapping("F1", [0d]);
        var recognizer = new DynamicIconRecognizer((_, _) =>
        {
            calls++;
            return new(mapping, 0);
        });
        recognizer.Match([0d], [mapping]);

        recognizer.Reset();
        recognizer.Match([0d], [mapping]);

        Assert.Equal(2, calls);
    }

    [Fact]
    public void Pixel_templates_switch_immediately_and_reject_unknown_frames()
    {
        var ring = PixelMapping("F2", TestIconFrames.Ring(108, 108));
        var diagonal = PixelMapping("1", TestIconFrames.Diagonal(108, 108));
        var recognizer = new DynamicIconRecognizer();

        var first = recognizer.Match(PixelIconTemplateBuilder.Create(TestIconFrames.Ring(76, 80), 76, 80), [ring, diagonal]);
        var second = recognizer.Match(PixelIconTemplateBuilder.Create(TestIconFrames.Diagonal(76, 80), 76, 80), [ring, diagonal]);
        var unknown = recognizer.Match(PixelIconTemplateBuilder.Create(TestIconFrames.Cross(76, 80), 76, 80), [ring, diagonal]);

        Assert.Equal("F2", first?.Mapping.ActionKey);
        Assert.Equal("1", second?.Mapping.ActionKey);
        Assert.Null(unknown);
    }

    private static IconKeyMapping Mapping(string key, double[] signature) => new()
    {
        ActionKey = key,
        Signature = signature,
        PreviewPng = "png",
        MatchThreshold = .18
    };

    private static IconKeyMapping PixelMapping(string key, byte[] pixels)
    {
        var template = PixelIconTemplateBuilder.Create(pixels, 108, 108);
        return new IconKeyMapping
        {
            ActionKey = key,
            PreviewPng = "png",
            MatchThreshold = .18,
            PixelTemplateData = template.Serialize()
        };
    }
}
