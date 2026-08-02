using GameMacro.App.Detection;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Detection;

public sealed class NormalizedRegionTests
{
    [Fact]
    public void Converts_normalized_region_to_client_pixels()
    {
        var pixels = new NormalizedRegion(.25, .5, .1, .2).ToPixels(1000, 500);

        Assert.Equal(new PixelRegion(250, 250, 100, 100), pixels);
    }

    [Fact]
    public void Rejects_region_outside_client_area()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new NormalizedRegion(.95, .1, .1, .1).ToPixels(1000, 500));
    }

    [Fact]
    public void Profile_shared_region_converts_to_client_pixels()
    {
        var profile = new MacroProfile
        {
            DetectionX = .5,
            DetectionY = .25,
            DetectionWidth = .1,
            DetectionHeight = .2
        };

        var pixels = SharedDetectionRegion.ToPixels(profile, 1000, 800);

        Assert.Equal(new PixelRegion(500, 200, 100, 160), pixels);
    }

    [Fact]
    public void Profile_skill_source_region_converts_to_client_pixels()
    {
        var profile = new MacroProfile
        {
            SourceX = .1,
            SourceY = .5,
            SourceWidth = .8,
            SourceHeight = .25
        };

        var pixels = SourceDetectionRegion.ToPixels(profile, 1000, 800);

        Assert.Equal(new PixelRegion(100, 400, 800, 200), pixels);
    }
}
