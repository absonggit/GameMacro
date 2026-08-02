using GameMacro.App.Overlay;

namespace GameMacro.App.Tests.Overlay;

public sealed class OverlayPresentationTests
{
    [Fact]
    public void Uses_compact_overlay_dimensions()
    {
        Assert.Equal(200, OverlayPresentation.Width);
        Assert.Equal(40, OverlayPresentation.Height);
        Assert.Equal(18, OverlayPresentation.DragHandleWidth);
        Assert.Equal(96, OverlayPresentation.ProfileSelectorWidth);
    }

    [Theory]
    [InlineData(false, "F5", "启动 F5")]
    [InlineData(true, "F5", "停止 F5")]
    [InlineData(false, "", "启动")]
    public void ToggleLabel_includes_current_hotkey(bool running, string hotkey, string expected)
    {
        Assert.Equal(expected, OverlayPresentation.ToggleLabel(running, hotkey));
    }
}
