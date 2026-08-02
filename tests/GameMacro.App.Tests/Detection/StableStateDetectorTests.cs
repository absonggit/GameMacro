using GameMacro.App.Detection;

namespace GameMacro.App.Tests.Detection;

public sealed class StableStateDetectorTests
{
    [Fact]
    public void Confirms_only_after_required_consecutive_samples()
    {
        var detector = new StableStateDetector(3);

        Assert.False(detector.Observe(IconVisualState.Ready));
        Assert.False(detector.Observe(IconVisualState.Unknown));
        Assert.False(detector.Observe(IconVisualState.Ready));
        Assert.False(detector.Observe(IconVisualState.Ready));
        Assert.True(detector.Observe(IconVisualState.Ready));
        Assert.Equal(IconVisualState.Ready, detector.ConfirmedState);
    }
}
