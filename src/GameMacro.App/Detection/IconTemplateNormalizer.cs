namespace GameMacro.App.Detection;

public sealed record NormalizedIcon(byte[] Pixels, int Width, int Height);

public static class IconTemplateNormalizer
{
    public static NormalizedIcon Normalize(byte[] bgra, int width, int height)
    {
        if (width < 12 || height < 12 || bgra.Length < width * height * 4)
            throw new ArgumentException("技能图标尺寸过小或像素数据无效。", nameof(bgra));

        var left = (int)Math.Round(width * .10);
        var top = (int)Math.Round(height * .08);
        var right = (int)Math.Round(width * .90);
        var bottom = (int)Math.Round(height * .72);
        var normalizedWidth = Math.Max(8, right - left);
        var normalizedHeight = Math.Max(8, bottom - top);
        var pixels = BgraFrameCropper.Crop(bgra, width, height, left, top, normalizedWidth, normalizedHeight);
        return new(pixels, normalizedWidth, normalizedHeight);
    }

    public static double[] CreateSignature(byte[] bgra, int width, int height)
    {
        var normalized = Normalize(bgra, width, height);
        return IconVisualSignature.Create(normalized.Pixels, normalized.Width, normalized.Height);
    }
}
