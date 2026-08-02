using GameMacro.Core.Models;

namespace GameMacro.App.Detection;

public static class SourceDetectionRegion
{
    public static PixelRegion ToPixels(MacroProfile profile, int clientWidth, int clientHeight)
        => new NormalizedRegion(profile.SourceX, profile.SourceY,
            profile.SourceWidth, profile.SourceHeight).ToPixels(clientWidth, clientHeight);
}
