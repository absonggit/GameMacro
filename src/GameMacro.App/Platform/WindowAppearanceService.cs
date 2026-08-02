using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GameMacro.App.Platform;

internal static class WindowAppearanceService
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;

    internal static void ApplyDarkTitleBar(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == 0) return;

        try
        {
            var enabled = 1;
            if (DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int)) != 0)
                DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeBefore20H1, ref enabled, sizeof(int));

            var borderColor = ToColorRef(0x28, 0x36, 0x4A);
            var captionColor = ToColorRef(0x0B, 0x10, 0x18);
            var textColor = ToColorRef(0xEA, 0xF0, 0xFA);
            DwmSetWindowAttribute(handle, DwmwaBorderColor, ref borderColor, sizeof(int));
            DwmSetWindowAttribute(handle, DwmwaCaptionColor, ref captionColor, sizeof(int));
            DwmSetWindowAttribute(handle, DwmwaTextColor, ref textColor, sizeof(int));
        }
        catch (DllNotFoundException)
        {
            // Older Windows installations without DWM retain the native title bar.
        }
        catch (EntryPointNotFoundException)
        {
            // Unsupported Windows versions retain the native title bar.
        }
    }

    private static int ToColorRef(byte red, byte green, byte blue)
        => red | (green << 8) | (blue << 16);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        nint windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);
}
