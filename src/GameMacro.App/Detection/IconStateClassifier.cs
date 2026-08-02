namespace GameMacro.App.Detection;

public enum IconVisualState { Unknown, Ready, Cooldown }

public static class IconStateClassifier
{
    public static IconVisualState Classify(double[] sample, double[] ready, double readyThreshold, double changeThreshold)
    {
        if (sample.Length == 0 || sample.Length != ready.Length || readyThreshold <= 0 || changeThreshold <= readyThreshold)
            return IconVisualState.Unknown;
        readyThreshold = Math.Clamp(readyThreshold, .08, .18);
        changeThreshold = Math.Clamp(readyThreshold + .05, .13, .23);
        var distance = Distance(sample, ready);
        if (distance <= readyThreshold) return IconVisualState.Ready;
        if (distance >= changeThreshold) return IconVisualState.Cooldown;
        return IconVisualState.Unknown;
    }

    public static IconVisualState Classify(double[] sample, double[] ready, double[] cooldown)
    {
        if (sample.Length == 0 || sample.Length != ready.Length || sample.Length != cooldown.Length)
            return IconVisualState.Unknown;
        var readyDistance = Distance(sample, ready);
        var cooldownDistance = Distance(sample, cooldown);
        if (Math.Abs(readyDistance - cooldownDistance) < .05) return IconVisualState.Unknown;
        var smaller = Math.Min(readyDistance, cooldownDistance);
        var larger = Math.Max(readyDistance, cooldownDistance);
        if (larger == 0 || smaller / larger > .85) return IconVisualState.Unknown;
        return readyDistance < cooldownDistance ? IconVisualState.Ready : IconVisualState.Cooldown;
    }

    public static double[] CreateSignature(byte[] bgra, int width, int height, int gridSize = 8)
    {
        if (width <= 0 || height <= 0 || bgra.Length < width * height * 4)
            throw new ArgumentException("截图数据尺寸无效。", nameof(bgra));
        var values = new double[gridSize * gridSize + 2];
        for (var gy = 0; gy < gridSize; gy++)
        for (var gx = 0; gx < gridSize; gx++)
        {
            var x = Math.Min(width - 1, (gx * width + width / 2) / gridSize);
            var y = Math.Min(height - 1, (gy * height + height / 2) / gridSize);
            var offset = (y * width + x) * 4;
            values[gy * gridSize + gx] = (bgra[offset] * .114 + bgra[offset + 1] * .587 + bgra[offset + 2] * .299) / 255;
        }
        var sampleCount = gridSize * gridSize;
        var mean = values.Take(sampleCount).Average();
        var scale = Math.Sqrt(values.Take(sampleCount).Sum(value => Math.Pow(value - mean, 2)) / sampleCount);
        if (scale < .001) scale = 1;
        for (var i = 0; i < sampleCount; i++) values[i] = (values[i] - mean) / scale;
        values[sampleCount] = mean * 8;
        values[sampleCount + 1] = scale * 4;
        return values;
    }

    public static double Distance(double[] left, double[] right)
    {
        var sum = 0d;
        for (var i = 0; i < left.Length; i++) sum += Math.Pow(left[i] - right[i], 2);
        return Math.Sqrt(sum / left.Length);
    }
}
