using GameMacro.Core.Models;

namespace GameMacro.App.Detection;

public static class SharedDetectionRegion
{
    public static PixelRegion ToPixels(MacroProfile profile, int clientWidth, int clientHeight)
        => new NormalizedRegion(profile.DetectionX, profile.DetectionY,
            profile.DetectionWidth, profile.DetectionHeight).ToPixels(clientWidth, clientHeight);
}
