namespace GameMacro.App.Detection;

public static class PixelIconTemplateBuilder
{
    public static PixelIconTemplate Create(byte[] bgra, int width, int height)
    {
        if (width < 12 || height < 12 || bgra.Length < width * height * 4)
            throw new ArgumentException("技能图标尺寸过小或像素数据无效。", nameof(bgra));

        var left = (int)Math.Round(width * .10);
        var top = (int)Math.Round(height * .12);
        var right = Math.Max(left + 1, (int)Math.Round(width * .90));
        var bottom = Math.Max(top + 1, (int)Math.Round(height * .80));
        var cropWidth = right - left;
        var cropHeight = bottom - top;
        var rgb = new byte[PixelIconTemplate.Size * PixelIconTemplate.Size * 3];
        for (var y = 0; y < PixelIconTemplate.Size; y++)
        for (var x = 0; x < PixelIconTemplate.Size; x++)
        {
            var sourceX = left + Math.Min(cropWidth - 1, (x * 2 + 1) * cropWidth / (PixelIconTemplate.Size * 2));
            var sourceY = top + Math.Min(cropHeight - 1, (y * 2 + 1) * cropHeight / (PixelIconTemplate.Size * 2));
            var source = (sourceY * width + sourceX) * 4;
            var target = (y * PixelIconTemplate.Size + x) * 3;
            rgb[target] = bgra[source + 2];
            rgb[target + 1] = bgra[source + 1];
            rgb[target + 2] = bgra[source];
        }
        return new(PixelIconTemplate.CurrentVersion, rgb);
    }
}
