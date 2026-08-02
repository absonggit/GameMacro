namespace GameMacro.App.Detection;

public readonly record struct PixelRegion(int X, int Y, int Width, int Height);

public readonly record struct NormalizedRegion(double X, double Y, double Width, double Height)
{
    public PixelRegion ToPixels(int clientWidth, int clientHeight)
    {
        if (clientWidth <= 0 || clientHeight <= 0 || X < 0 || Y < 0 || Width <= 0 || Height <= 0
            || X + Width > 1 || Y + Height > 1)
            throw new InvalidOperationException("技能检测区域超出游戏客户区。");
        return new(
            (int)Math.Round(X * clientWidth), (int)Math.Round(Y * clientHeight),
            Math.Max(1, (int)Math.Round(Width * clientWidth)),
            Math.Max(1, (int)Math.Round(Height * clientHeight)));
    }
}
