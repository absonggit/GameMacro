namespace GameMacro.App.Platform;

public sealed class PhysicalKeyboardEventArgs(ushort virtualKey, bool isDown) : EventArgs
{
    public ushort VirtualKey { get; } = virtualKey;
    public bool IsDown { get; } = isDown;
}
