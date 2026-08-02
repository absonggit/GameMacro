using System.Runtime.InteropServices;

namespace GameMacro.App.Platform;

public static class SendInputLayout
{
    public static int Size => Marshal.SizeOf<NativeMethods.Input>();
}
