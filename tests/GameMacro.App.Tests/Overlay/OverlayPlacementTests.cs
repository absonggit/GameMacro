using GameMacro.App.Overlay;

namespace GameMacro.App.Tests.Overlay;

public sealed class OverlayPlacementTests
{
    [Fact]
    public void Normalized_position_round_trips_through_screen_coordinates()
    {
        var client = new OverlayBounds(100, 200, 1200, 800);

        var screen = OverlayPlacement.ToScreen(0.25, 0.75, client, 300, 80);
        var normalized = OverlayPlacement.ToNormalized(
            screen.X, screen.Y, client, 300, 80);

        Assert.Equal(0.25, normalized.Left, 6);
        Assert.Equal(0.75, normalized.Top, 6);
    }

    [Fact]
    public void ToScreen_clamps_overlay_inside_client_area()
    {
        var client = new OverlayBounds(50, 60, 400, 200);

        var screen = OverlayPlacement.ToScreen(2, -1, client, 150, 80);

        Assert.Equal(300, screen.X);
        Assert.Equal(60, screen.Y);
    }

    [Fact]
    public void ToScreen_uses_small_top_left_offset_when_position_missing()
    {
        var client = new OverlayBounds(100, 200, 1000, 500);

        var screen = OverlayPlacement.ToScreen(null, null, client, 300, 80);

        Assert.Equal(114, screen.X);
        Assert.Equal(208.4, screen.Y, 6);
    }

    [Fact]
    public void Oversized_overlay_anchors_to_client_top_left()
    {
        var client = new OverlayBounds(10, 20, 100, 50);

        var screen = OverlayPlacement.ToScreen(1, 1, client, 300, 80);

        Assert.Equal(10, screen.X);
        Assert.Equal(20, screen.Y);
    }
}
