namespace GameMacro.App.Detection;

public static class IconVisualSignature
{
    private const double VersionMarker = 20260720;
    private const int Grid = 8;
    private const int Cells = Grid * Grid;
    private const int HueBins = 24;
    private const int LuminanceOffset = 1;
    private const int RedGreenOffset = LuminanceOffset + Cells;
    private const int BlueGreenOffset = RedGreenOffset + Cells;
    private const int EdgeOffset = BlueGreenOffset + Cells;
    private const int MeanOffset = EdgeOffset + Cells;
    private const int ScaleOffset = MeanOffset + 1;
    private const int HueOffset = ScaleOffset + 1;
    public const int Length = HueOffset + HueBins;

    public static bool IsCurrent(double[]? signature)
        => signature is { Length: Length } && Math.Abs(signature[0] - VersionMarker) < .5;

    public static bool IsLikelyEmpty(double[] signature)
    {
        if (!IsCurrent(signature)) return false;
        var averageEdge = 0d;
        for (var index = 0; index < Cells; index++) averageEdge += signature[EdgeOffset + index];
        averageEdge /= Cells;
        return signature[ScaleOffset] < .04 && averageEdge < .035;
    }

    public static double[] Create(byte[] bgra, int width, int height)
    {
        if (width <= 0 || height <= 0 || bgra.Length < width * height * 4)
            throw new ArgumentException("截图数据尺寸无效。", nameof(bgra));

        var luminance = new double[Cells];
        var redGreen = new double[Cells];
        var blueGreen = new double[Cells];
        for (var gy = 0; gy < Grid; gy++)
        for (var gx = 0; gx < Grid; gx++)
        {
            var left = gx * width / Grid;
            var right = Math.Max(left + 1, (gx + 1) * width / Grid);
            var top = gy * height / Grid;
            var bottom = Math.Max(top + 1, (gy + 1) * height / Grid);
            double red = 0, green = 0, blue = 0;
            var count = 0;
            for (var y = top; y < Math.Min(bottom, height); y++)
            for (var x = left; x < Math.Min(right, width); x++)
            {
                var offset = (y * width + x) * 4;
                blue += bgra[offset];
                green += bgra[offset + 1];
                red += bgra[offset + 2];
                count++;
            }
            var index = gy * Grid + gx;
            red /= count;
            green /= count;
            blue /= count;
            luminance[index] = (blue * .114 + green * .587 + red * .299) / 255;
            redGreen[index] = (red - green) / 255;
            blueGreen[index] = (blue - green) / 255;
        }

        var mean = luminance.Average();
        var scale = Math.Sqrt(luminance.Sum(value => Math.Pow(value - mean, 2)) / Cells);
        if (scale < .01) scale = .01;
        var result = new double[Length];
        result[0] = VersionMarker;
        for (var index = 0; index < Cells; index++)
        {
            result[LuminanceOffset + index] = (luminance[index] - mean) / scale;
            result[RedGreenOffset + index] = redGreen[index];
            result[BlueGreenOffset + index] = blueGreen[index];
            var x = index % Grid;
            var y = index / Grid;
            var horizontal = x + 1 < Grid ? Math.Abs(luminance[index + 1] - luminance[index]) : 0;
            var vertical = y + 1 < Grid ? Math.Abs(luminance[index + Grid] - luminance[index]) : 0;
            result[EdgeOffset + index] = horizontal + vertical;
        }
        result[MeanOffset] = mean;
        result[ScaleOffset] = scale;
        FillHueHistogram(bgra, width, height, result);
        return result;
    }

    public static double Distance(double[] left, double[] right)
    {
        if (!IsCurrent(left) || !IsCurrent(right))
            return left.Length == right.Length ? IconStateClassifier.Distance(left, right) : double.PositiveInfinity;

        var structure = ShiftTolerantStructureDistance(left, right);
        var brightness = Math.Abs(left[MeanOffset] - right[MeanOffset]);
        var contrast = Math.Abs(left[ScaleOffset] - right[ScaleOffset]);
        var hue = HistogramDistance(left, right, HueOffset, HueBins);
        return hue * .40 + structure * .48 + brightness * .06 + contrast * .06;
    }

    private static double ShiftTolerantStructureDistance(double[] left, double[] right)
    {
        var best = double.PositiveInfinity;
        for (var offsetY = -2; offsetY <= 2; offsetY++)
        for (var offsetX = -2; offsetX <= 2; offsetX++)
        {
            var score = StructureDistanceAtOffset(left, right, offsetX, offsetY)
                + (Math.Abs(offsetX) + Math.Abs(offsetY)) * .008;
            best = Math.Min(best, score);
        }
        var fixedPosition = StructureDistanceAtOffset(left, right, 0, 0);
        return best * .50 + fixedPosition * .50;
    }

    private static double StructureDistanceAtOffset(double[] left, double[] right, int offsetX, int offsetY)
    {
        double luminance = 0, color = 0, edge = 0;
        var count = 0;
        for (var y = 0; y < Grid; y++)
        for (var x = 0; x < Grid; x++)
        {
            var otherX = x + offsetX;
            var otherY = y + offsetY;
            if (otherX is < 0 or >= Grid || otherY is < 0 or >= Grid) continue;
            var leftIndex = y * Grid + x;
            var rightIndex = otherY * Grid + otherX;
            luminance += Square(left[LuminanceOffset + leftIndex] - right[LuminanceOffset + rightIndex]);
            color += Square(left[RedGreenOffset + leftIndex] - right[RedGreenOffset + rightIndex]);
            color += Square(left[BlueGreenOffset + leftIndex] - right[BlueGreenOffset + rightIndex]);
            edge += Square(left[EdgeOffset + leftIndex] - right[EdgeOffset + rightIndex]);
            count++;
        }
        return Math.Sqrt(luminance / count) * .58
            + Math.Sqrt(color / (count * 2)) * .17
            + Math.Sqrt(edge / count) * .25;
    }

    private static double Square(double value) => value * value;

    private static void FillHueHistogram(byte[] bgra, int width, int height, double[] signature)
    {
        var total = 0d;
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var offset = (y * width + x) * 4;
            var blue = bgra[offset] / 255d;
            var green = bgra[offset + 1] / 255d;
            var red = bgra[offset + 2] / 255d;
            var maximum = Math.Max(red, Math.Max(green, blue));
            var minimum = Math.Min(red, Math.Min(green, blue));
            var delta = maximum - minimum;
            var saturation = maximum <= 0 ? 0 : delta / maximum;
            if (saturation < .12 || maximum < .08 || delta <= 0) continue;
            double hue;
            if (maximum == red) hue = ((green - blue) / delta % 6) / 6;
            else if (maximum == green) hue = ((blue - red) / delta + 2) / 6;
            else hue = ((red - green) / delta + 4) / 6;
            if (hue < 0) hue += 1;
            var bin = Math.Min(HueBins - 1, (int)(hue * HueBins));
            var weight = saturation * maximum;
            signature[HueOffset + bin] += weight;
            total += weight;
        }
        if (total <= 0) return;
        for (var bin = 0; bin < HueBins; bin++) signature[HueOffset + bin] /= total;
    }

    private static double HistogramDistance(double[] left, double[] right, int offset, int count)
    {
        var sum = 0d;
        for (var index = 0; index < count; index++)
            sum += Math.Abs(left[offset + index] - right[offset + index]);
        return sum / 2;
    }

    private static double Rmse(double[] left, double[] right, int offset, int count)
    {
        var sum = 0d;
        for (var index = 0; index < count; index++)
            sum += Math.Pow(left[offset + index] - right[offset + index], 2);
        return Math.Sqrt(sum / count);
    }
}
