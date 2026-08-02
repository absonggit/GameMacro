using GameMacro.App.Detection;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Detection;

public sealed class BatchMappingBuilderTests
{
    [Fact]
    public void Building_pending_cards_does_not_mutate_saved_mappings()
    {
        var profile = new MacroProfile
        {
            IconMappings = [new IconKeyMapping { ActionKey = "F9", Signature = [9d], PreviewPng = "old", MatchThreshold = .1 }]
        };

        var pending = BatchMappingBuilder.Build([Detected("new", [1d])]);

        Assert.Single(pending);
        Assert.Equal("F9", profile.IconMappings.Single().ActionKey);
        Assert.Equal("old", profile.IconMappings.Single().PreviewPng);
    }

    [Fact]
    public void Saving_requires_a_key_for_every_pending_icon()
    {
        var pending = BatchMappingBuilder.Build([Detected("icon", [1d])]);

        var exception = Assert.Throws<InvalidOperationException>(() => BatchMappingBuilder.Save(pending));

        Assert.Contains("按键", exception.Message);
    }

    [Fact]
    public void Saving_converts_every_pending_card_to_calibrated_runtime_mapping()
    {
        var pending = BatchMappingBuilder.Build([Detected("a", [1d]), Detected("b", [2d])]);
        pending[0].ActionKey = "F1";
        pending[1].ActionKey = "2";

        var mappings = BatchMappingBuilder.Save(pending);

        Assert.Equal(["F1", "2"], mappings.Select(mapping => mapping.ActionKey));
        Assert.All(mappings, mapping => Assert.True(mapping.IsCalibrated));
        Assert.Equal("a", mappings[0].PreviewPng);
        Assert.Equal([2d], mappings[1].Signature);
    }

    private static DetectedSkillIcon Detected(string preview, double[] signature) => new(
        new PixelRegion(0, 0, 32, 32), new byte[32 * 32 * 4], 32, 32, preview, signature, .18);
}
