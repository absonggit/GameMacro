namespace GameMacro.App.Overlay;

public readonly record struct OverlayBounds(double X, double Y, double Width, double Height);

public readonly record struct OverlayPoint(double X, double Y);

public readonly record struct NormalizedOverlayPoint(double Left, double Top);

public static class OverlayPlacement
{
    private const double DefaultPosition = 0.02;

    public static OverlayPoint ToScreen(
        double? normalizedLeft,
        double? normalizedTop,
        OverlayBounds client,
        double overlayWidth,
        double overlayHeight)
    {
        var availableWidth = Math.Max(0, client.Width - overlayWidth);
        var availableHeight = Math.Max(0, client.Height - overlayHeight);
        var left = Math.Clamp(normalizedLeft ?? DefaultPosition, 0, 1);
        var top = Math.Clamp(normalizedTop ?? DefaultPosition, 0, 1);

        return new OverlayPoint(
            client.X + availableWidth * left,
            client.Y + availableHeight * top);
    }

    public static NormalizedOverlayPoint ToNormalized(
        double screenLeft,
        double screenTop,
        OverlayBounds client,
        double overlayWidth,
        double overlayHeight)
    {
        var availableWidth = Math.Max(0, client.Width - overlayWidth);
        var availableHeight = Math.Max(0, client.Height - overlayHeight);
        var left = availableWidth == 0 ? 0 : (screenLeft - client.X) / availableWidth;
        var top = availableHeight == 0 ? 0 : (screenTop - client.Y) / availableHeight;

        return new NormalizedOverlayPoint(
            Math.Clamp(left, 0, 1),
            Math.Clamp(top, 0, 1));
    }
}
