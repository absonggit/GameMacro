using GameMacro.Core.Models;

namespace GameMacro.App.Detection;

public sealed record PixelIconCandidate(IconKeyMapping Mapping, PixelIconTemplate Template);
public sealed record PixelIconMatch(IconKeyMapping Mapping, double Distance, double RunnerUpDistance);

public static class PixelIconTemplateMatcher
{
    private const double MaximumDistance = .30;
    private const double MinimumLead = .025;

    public static PixelIconMatch? Match(PixelIconTemplate sample, IEnumerable<PixelIconCandidate> candidates)
    {
        var ranked = candidates
            .Where(candidate => candidate.Mapping.Enabled)
            .Select(candidate => new { candidate.Mapping, Distance = Distance(sample, candidate.Template) })
            .OrderBy(item => item.Distance)
            .ThenBy(item => item.Mapping.Id)
            .Take(2)
            .ToList();
        if (ranked.Count == 0 || ranked[0].Distance > MaximumDistance) return null;
        var runnerUp = ranked.Count > 1 ? ranked[1].Distance : double.PositiveInfinity;
        if (runnerUp - ranked[0].Distance < MinimumLead) return null;
        return new(ranked[0].Mapping, ranked[0].Distance, runnerUp);
    }

    public static double Distance(PixelIconTemplate left, PixelIconTemplate right)
    {
        if (left.Version != PixelIconTemplate.CurrentVersion || right.Version != PixelIconTemplate.CurrentVersion)
            return double.PositiveInfinity;
        var orientation = GradientOrientationDistance(left.Rgb, right.Rgb);
        var best = double.PositiveInfinity;
        for (var offsetY = -2; offsetY <= 2; offsetY++)
        for (var offsetX = -2; offsetX <= 2; offsetX++)
        {
            var score = DistanceAtOffset(left.Rgb, right.Rgb, offsetX, offsetY)
                + (Math.Abs(offsetX) + Math.Abs(offsetY)) * .004;
            best = Math.Min(best, score);
        }
        return best * .82 + orientation * .18;
    }

    private static double DistanceAtOffset(byte[] left, byte[] right, int offsetX, int offsetY)
    {
        double sumLeft = 0, sumRight = 0;
        double redLeft = 0, greenLeft = 0, blueLeft = 0;
        double redRight = 0, greenRight = 0, blueRight = 0;
        var count = 0;
        for (var y = 5; y < PixelIconTemplate.Size - 3; y++)
        for (var x = 2; x < PixelIconTemplate.Size - 2; x++)
        {
            var otherX = x + offsetX;
            var otherY = y + offsetY;
            if (otherX is < 2 or >= PixelIconTemplate.Size - 2
                || otherY is < 5 or >= PixelIconTemplate.Size - 3
                || !IsComparablePixel(x, y)
                || !IsComparablePixel(otherX, otherY)) continue;
            var li = (y * PixelIconTemplate.Size + x) * 3;
            var ri = (otherY * PixelIconTemplate.Size + otherX) * 3;
            sumLeft += Luminance(left, li);
            sumRight += Luminance(right, ri);
            redLeft += left[li]; greenLeft += left[li + 1]; blueLeft += left[li + 2];
            redRight += right[ri]; greenRight += right[ri + 1]; blueRight += right[ri + 2];
            count++;
        }
        if (count < 100) return double.PositiveInfinity;
        var meanLeft = sumLeft / count;
        var meanRight = sumRight / count;
        double covariance = 0, varianceLeft = 0, varianceRight = 0;
        double edgeDifference = 0;
        for (var y = 5; y < PixelIconTemplate.Size - 3; y++)
        for (var x = 2; x < PixelIconTemplate.Size - 2; x++)
        {
            var otherX = x + offsetX;
            var otherY = y + offsetY;
            if (otherX is < 2 or >= PixelIconTemplate.Size - 2
                || otherY is < 5 or >= PixelIconTemplate.Size - 3
                || !IsComparablePixel(x, y)
                || !IsComparablePixel(otherX, otherY)) continue;
            var li = (y * PixelIconTemplate.Size + x) * 3;
            var ri = (otherY * PixelIconTemplate.Size + otherX) * 3;
            var lv = Luminance(left, li) - meanLeft;
            var rv = Luminance(right, ri) - meanRight;
            covariance += lv * rv;
            varianceLeft += lv * lv;
            varianceRight += rv * rv;
            edgeDifference += Math.Abs(Edge(left, x, y) - Edge(right, otherX, otherY));
        }
        var correlation = covariance / Math.Sqrt(Math.Max(1, varianceLeft * varianceRight));
        var structure = (1 - Math.Clamp(correlation, -1, 1)) / 2;
        var edge = edgeDifference / (count * 255d);
        var color = ColorDistance(
            redLeft / count, greenLeft / count, blueLeft / count,
            redRight / count, greenRight / count, blueRight / count);
        return structure * .67 + edge * .18 + color * .15;
    }

    private static double GradientOrientationDistance(byte[] left, byte[] right)
    {
        const int binCount = 8;
        var leftBins = new double[binCount];
        var rightBins = new double[binCount];
        AddGradientOrientations(left, leftBins);
        AddGradientOrientations(right, rightBins);
        var leftTotal = leftBins.Sum();
        var rightTotal = rightBins.Sum();
        if (leftTotal < 1 && rightTotal < 1) return 0;
        if (leftTotal < 1 || rightTotal < 1) return 1;
        double difference = 0;
        for (var index = 0; index < binCount; index++)
            difference += Math.Abs(leftBins[index] / leftTotal - rightBins[index] / rightTotal);
        return difference / 2;
    }

    private static void AddGradientOrientations(byte[] rgb, double[] bins)
    {
        for (var y = 6; y < PixelIconTemplate.Size - 3; y++)
        for (var x = 3; x < PixelIconTemplate.Size - 3; x++)
        {
            if (!IsStablePixel(x, y)
                || !IsStablePixel(x - 1, y)
                || !IsStablePixel(x + 1, y)
                || !IsStablePixel(x, y - 1)
                || !IsStablePixel(x, y + 1)) continue;
            var dx = Luminance(rgb, (y * PixelIconTemplate.Size + x + 1) * 3)
                - Luminance(rgb, (y * PixelIconTemplate.Size + x - 1) * 3);
            var dy = Luminance(rgb, ((y + 1) * PixelIconTemplate.Size + x) * 3)
                - Luminance(rgb, ((y - 1) * PixelIconTemplate.Size + x) * 3);
            var magnitude = Math.Sqrt(dx * dx + dy * dy);
            if (magnitude < 12) continue;
            var angle = Math.Atan2(dy, dx);
            if (angle < 0) angle += Math.PI;
            if (angle >= Math.PI) angle -= Math.PI;
            var bin = Math.Min(bins.Length - 1, (int)(angle / Math.PI * bins.Length));
            bins[bin] += magnitude;
        }
    }

    private static bool IsComparablePixel(int x, int y)
        => IsStablePixel(x, y) && IsStablePixel(x + 1, y) && IsStablePixel(x, y + 1);

    private static bool IsStablePixel(int x, int y)
        => x < 19 || y > 18;

    private static double ColorDistance(double lr, double lg, double lb, double rr, double rg, double rb)
    {
        var leftTotal = Math.Max(1, lr + lg + lb);
        var rightTotal = Math.Max(1, rr + rg + rb);
        return (Math.Abs(lr / leftTotal - rr / rightTotal)
            + Math.Abs(lg / leftTotal - rg / rightTotal)
            + Math.Abs(lb / leftTotal - rb / rightTotal)) / 2;
    }

    private static double Luminance(byte[] rgb, int index)
        => rgb[index] * .299 + rgb[index + 1] * .587 + rgb[index + 2] * .114;

    private static double Edge(byte[] rgb, int x, int y)
    {
        var center = (y * PixelIconTemplate.Size + x) * 3;
        var right = center + 3;
        var down = center + PixelIconTemplate.Size * 3;
        return Math.Abs(Luminance(rgb, right) - Luminance(rgb, center))
            + Math.Abs(Luminance(rgb, down) - Luminance(rgb, center));
    }
}
