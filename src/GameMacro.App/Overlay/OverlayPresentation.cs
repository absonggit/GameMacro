namespace GameMacro.App.Overlay;

public static class OverlayPresentation
{
    public const double Width = 200;
    public const double Height = 40;
    public const double DragHandleWidth = 18;
    public const double ProfileSelectorWidth = 96;

    public static string ToggleLabel(bool running, string? hotkey)
    {
        var action = running ? "停止" : "启动";
        return string.IsNullOrWhiteSpace(hotkey) ? action : $"{action} {hotkey.Trim()}";
    }
}
