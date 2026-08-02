using System.Runtime.InteropServices;

namespace GameMacro.App.Platform;

public sealed class PhysicalKeyboardMonitor : IDisposable
{
    private readonly NativeMethods.LowLevelKeyboardProc _callback;
    private nint _hook;

    public PhysicalKeyboardMonitor() => _callback = HookCallback;

    public event EventHandler<PhysicalKeyboardEventArgs>? KeyChanged;

    public bool IsRunning => _hook != 0;

    public bool Start()
    {
        if (IsRunning) return true;
        _hook = NativeMethods.SetWindowsHookEx(
            NativeMethods.WhKeyboardLl,
            _callback,
            NativeMethods.GetModuleHandle(null),
            0);
        return IsRunning;
    }

    public void Stop()
    {
        if (_hook == 0) return;
        NativeMethods.UnhookWindowsHookEx(_hook);
        _hook = 0;
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    private nint HookCallback(int code, nint wParam, nint lParam)
    {
        try
        {
            if (code >= 0)
            {
                var data = Marshal.PtrToStructure<NativeMethods.LowLevelKeyboardInput>(lParam);
                if (PhysicalKeyboardEventDecoder.TryDecode(
                    code,
                    unchecked((int)(long)wParam),
                    data.VirtualKey,
                    data.Flags,
                    out var value))
                    KeyChanged?.Invoke(this, value!);
            }
        }
        catch
        {
            // A keyboard hook must never prevent the original event reaching the game.
        }
        return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
    }
}
