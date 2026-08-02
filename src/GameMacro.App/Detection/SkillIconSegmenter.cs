namespace GameMacro.App.Detection;

public sealed record DetectedSkillIcon(
    PixelRegion Region,
    byte[] Pixels,
    int Width,
    int Height,
    string PreviewPng,
    double[] Signature,
    double MatchThreshold);

public sealed record SkillIconSegmentationResult(
    IReadOnlyList<DetectedSkillIcon> Icons,
    int CandidateCount,
    int EmptyFilteredCount,
    int DuplicateCount);

public static class SkillIconSegmenter
{
    private const int MinimumSize = 18;
    private const double DuplicateDistance = .06;

    public static SkillIconSegmentationResult Segment(byte[] bgra, int width, int height)
    {
        if (width <= 0 || height <= 0 || bgra.Length < width * height * 4)
            throw new ArgumentException("技能来源截图尺寸无效。", nameof(bgra));

        var edgeMask = CreateEdgeMask(bgra, width, height);
        var connectedMask = Dilate(edgeMask, width, height, 2);
        var components = FindComponents(connectedMask, width, height);
        var squareCandidates = components
            .Where(region => region.Width >= MinimumSize && region.Height >= MinimumSize)
            .Where(region => region.Width <= width * .65 && region.Height <= height)
            .Where(region => region.Width / (double)region.Height is >= .68 and <= 1.47)
            .Select(region => Clamp(region, width, height))
            .ToList();
        squareCandidates = squareCandidates
            .Where(candidate => !squareCandidates.Any(container => IsNestedCandidate(candidate, container)))
            .ToList();

        List<DetectedSkillIcon> textured = [];
        var emptyFiltered = 0;
        foreach (var region in squareCandidates)
        {
            var crop = BgraFrameCropper.Crop(bgra, width, height, region.X, region.Y, region.Width, region.Height);
            if (!HasIconTexture(crop, region.Width, region.Height))
            {
                emptyFiltered++;
                continue;
            }
            var signature = IconTemplateNormalizer.CreateSignature(crop, region.Width, region.Height);
            textured.Add(new(
                region,
                crop,
                region.Width,
                region.Height,
                PngPreviewCodec.EncodeBgra(crop, region.Width, region.Height),
                signature,
                .18));
        }

        var ordered = OrderByRows(textured);
        List<DetectedSkillIcon> unique = [];
        var duplicates = 0;
        foreach (var icon in ordered)
        {
            if (unique.Any(existing => existing.Signature.Length == icon.Signature.Length
                && IconVisualSignature.Distance(existing.Signature, icon.Signature) <= DuplicateDistance))
            {
                duplicates++;
                continue;
            }
            unique.Add(icon);
        }

        return new(unique, squareCandidates.Count, emptyFiltered, duplicates);
    }

    private static bool[] CreateEdgeMask(byte[] pixels, int width, int height)
    {
        var mask = new bool[width * height];
        for (var y = 1; y < height - 1; y++)
        for (var x = 1; x < width - 1; x++)
        {
            var current = (y * width + x) * 4;
            var left = current - 4;
            var top = current - width * 4;
            var horizontal = Math.Max(Math.Abs(pixels[current] - pixels[left]),
                Math.Max(Math.Abs(pixels[current + 1] - pixels[left + 1]), Math.Abs(pixels[current + 2] - pixels[left + 2])));
            var vertical = Math.Max(Math.Abs(pixels[current] - pixels[top]),
                Math.Max(Math.Abs(pixels[current + 1] - pixels[top + 1]), Math.Abs(pixels[current + 2] - pixels[top + 2])));
            mask[y * width + x] = Math.Max(horizontal, vertical) >= 14;
        }
        return mask;
    }

    private static bool[] Dilate(bool[] source, int width, int height, int radius)
    {
        var result = new bool[source.Length];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            if (!source[y * width + x]) continue;
            for (var dy = -radius; dy <= radius; dy++)
            for (var dx = -radius; dx <= radius; dx++)
            {
                var targetX = x + dx;
                var targetY = y + dy;
                if (targetX >= 0 && targetX < width && targetY >= 0 && targetY < height)
                    result[targetY * width + targetX] = true;
            }
        }
        return result;
    }

    private static List<PixelRegion> FindComponents(bool[] mask, int width, int height)
    {
        var visited = new bool[mask.Length];
        List<PixelRegion> regions = [];
        int[] offsetX = [1, -1, 0, 0];
        int[] offsetY = [0, 0, 1, -1];
        for (var start = 0; start < mask.Length; start++)
        {
            if (!mask[start] || visited[start]) continue;
            var queue = new Queue<int>();
            queue.Enqueue(start);
            visited[start] = true;
            var minX = start % width;
            var maxX = minX;
            var minY = start / width;
            var maxY = minY;
            var count = 0;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                count++;
                var x = current % width;
                var y = current / width;
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
                for (var direction = 0; direction < 4; direction++)
                {
                    var nextX = x + offsetX[direction];
                    var nextY = y + offsetY[direction];
                    if (nextX < 0 || nextX >= width || nextY < 0 || nextY >= height) continue;
                    var next = nextY * width + nextX;
                    if (!mask[next] || visited[next]) continue;
                    visited[next] = true;
                    queue.Enqueue(next);
                }
            }
            if (count >= 20) regions.Add(new(minX, minY, maxX - minX + 1, maxY - minY + 1));
        }
        return regions;
    }

    private static bool HasIconTexture(byte[] pixels, int width, int height)
    {
        var left = Math.Max(1, (int)(width * .15));
        var right = Math.Min(width - 1, (int)(width * .85));
        var top = Math.Max(1, (int)(height * .12));
        var bottom = Math.Min(height - 1, (int)(height * .70));
        var count = 0;
        var sum = 0d;
        var sumSquares = 0d;
        var edgeSum = 0d;
        for (var y = top; y < bottom; y++)
        for (var x = left; x < right; x++)
        {
            var offset = (y * width + x) * 4;
            var luminance = pixels[offset] * .114 + pixels[offset + 1] * .587 + pixels[offset + 2] * .299;
            var leftOffset = offset - 4;
            var leftLuminance = pixels[leftOffset] * .114 + pixels[leftOffset + 1] * .587 + pixels[leftOffset + 2] * .299;
            count++;
            sum += luminance;
            sumSquares += luminance * luminance;
            edgeSum += Math.Abs(luminance - leftLuminance);
        }
        if (count == 0) return false;
        var variance = sumSquares / count - Math.Pow(sum / count, 2);
        var averageEdge = edgeSum / count;
        return variance >= 120 || averageEdge >= 5;
    }

    private static PixelRegion Clamp(PixelRegion region, int width, int height)
    {
        var x = Math.Max(0, region.X);
        var y = Math.Max(0, region.Y);
        return new(x, y, Math.Min(width - x, region.Width), Math.Min(height - y, region.Height));
    }

    private static bool IsNestedCandidate(PixelRegion candidate, PixelRegion container)
    {
        if (candidate == container || container.Width * container.Height < candidate.Width * candidate.Height * 2) return false;
        var centerX = candidate.X + candidate.Width / 2d;
        var centerY = candidate.Y + candidate.Height / 2d;
        return centerX >= container.X && centerX <= container.X + container.Width
            && centerY >= container.Y && centerY <= container.Y + container.Height;
    }

    private static List<DetectedSkillIcon> OrderByRows(List<DetectedSkillIcon> icons)
    {
        var remaining = icons.OrderBy(icon => icon.Region.Y + icon.Region.Height / 2d).ToList();
        List<DetectedSkillIcon> result = [];
        while (remaining.Count > 0)
        {
            var first = remaining[0];
            var center = first.Region.Y + first.Region.Height / 2d;
            var tolerance = first.Region.Height * .45;
            var row = remaining.Where(icon => Math.Abs(icon.Region.Y + icon.Region.Height / 2d - center)
                <= Math.Max(tolerance, icon.Region.Height * .45)).OrderBy(icon => icon.Region.X).ToList();
            result.AddRange(row);
            foreach (var icon in row) remaining.Remove(icon);
        }
        return result;
    }
}
