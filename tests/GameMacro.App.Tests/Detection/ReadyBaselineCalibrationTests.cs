using GameMacro.App.Detection;

namespace GameMacro.App.Tests.Detection;

public sealed class ReadyBaselineCalibrationTests
{
    [Fact]
    public void Averages_frames_and_derives_separated_thresholds()
    {
        var result = ReadyBaselineCalibration.Create([[1, 1], [1.1, .9], [.9, 1.1]]);

        Assert.Equal(1, result.Baseline[0], 6);
        Assert.Equal(1, result.Baseline[1], 6);
        Assert.True(result.ReadyThreshold > 0);
        Assert.True(result.ChangeThreshold > result.ReadyThreshold);
        Assert.InRange(result.ReadyThreshold, .08, .18);
        Assert.InRange(result.ChangeThreshold, .13, .23);
    }
}
