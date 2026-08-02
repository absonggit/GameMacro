using GameMacro.App.Detection;
using GameMacro.App.ViewModels;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Detection;

public sealed class SingleIconMappingBuilderTests
{
    [Fact]
    public void Build_creates_one_pending_mapping_with_full_pixel_template()
    {
        var pixels = TestIconFrames.Ring(64, 64);
        var captured = new CapturedRegion(pixels, 64, 64, [0.1, 0.2], "png");

        var item = SingleIconMappingBuilder.Build(captured);

        Assert.Equal("png", item.PreviewPng);
        Assert.Equal([0.1, 0.2], item.Signature);
        Assert.NotNull(PixelIconTemplate.Deserialize(item.PixelTemplateData));
        Assert.DoesNotContain(item.ActionKey, InputKeyOptions.All);
    }

    [Fact]
    public void IsDuplicate_accepts_identical_signature_and_rejects_distinct_signature()
    {
        var existing = new PendingIconMapping { Signature = [0.1, 0.2, 0.3] };
        var identical = new PendingIconMapping { Signature = [0.1, 0.2, 0.3] };
        var distinct = new PendingIconMapping { Signature = [0.9, 0.8, 0.7] };

        Assert.True(SingleIconMappingBuilder.IsDuplicate(identical, [existing]));
        Assert.False(SingleIconMappingBuilder.IsDuplicate(distinct, [existing]));
    }
}
