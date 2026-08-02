namespace GameMacro.App.Detection;

public static class BgraFrameCropper
{
    public static byte[] Crop(byte[] source, int sourceWidth, int sourceHeight,
        int x, int y, int width, int height)
    {
        if (x < 0 || y < 0 || width <= 0 || height <= 0
            || x + width > sourceWidth || y + height > sourceHeight)
            throw new ArgumentOutOfRangeException(nameof(x), "裁剪区域超出截图范围。");
        var result = new byte[width * height * 4];
        var rowBytes = width * 4;
        for (var row = 0; row < height; row++)
            Buffer.BlockCopy(source, ((y + row) * sourceWidth + x) * 4,
                result, row * rowBytes, rowBytes);
        return result;
    }
}
