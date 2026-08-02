namespace GameMacro.App.Platform;

public static class PhysicalKeyboardEventDecoder
{
    public static bool TryDecode(
        int code,
        int message,
        uint virtualKey,
        uint flags,
        out PhysicalKeyboardEventArgs? value)
    {
        value = null;
        if (code < 0 || (flags & NativeMethods.LlkhfInjected) != 0) return false;
        var isDown = message is NativeMethods.WmKeyDown or NativeMethods.WmSysKeyDown;
        var isUp = message is NativeMethods.WmKeyUp or NativeMethods.WmSysKeyUp;
        if (!isDown && !isUp) return false;
        value = new((ushort)virtualKey, isDown);
        return true;
    }
}
