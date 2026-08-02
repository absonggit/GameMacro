namespace GameMacro.App.Detection;

public sealed record ReadyBaselineResult(double[] Baseline, double ReadyThreshold, double ChangeThreshold);

public static class ReadyBaselineCalibration
{
    public static ReadyBaselineResult Create(IReadOnlyList<double[]> frames)
    {
        if (frames.Count < 2 || frames[0].Length == 0 || frames.Any(frame => frame.Length != frames[0].Length))
            throw new ArgumentException("就绪标定至少需要两个相同尺寸的样本。", nameof(frames));
        var baseline = new double[frames[0].Length];
        foreach (var frame in frames)
        for (var index = 0; index < baseline.Length; index++) baseline[index] += frame[index] / frames.Count;
        var distances = frames.Select(frame => IconStateClassifier.Distance(frame, baseline)).Order().ToArray();
        var medianJitter = distances[distances.Length / 2];
        var readyThreshold = Math.Clamp(medianJitter * 2 + .02, .08, .18);
        var changeThreshold = Math.Clamp(readyThreshold + .05, .13, .23);
        return new(baseline, readyThreshold, changeThreshold);
    }
}
