using System.Diagnostics;
using GameMacro.Core.Models;
using GameMacro.Core.Runtime;

namespace GameMacro.App.Platform;

public sealed record WindowInfo(nint Handle, string Title, string ProcessName, int ClientWidth, int ClientHeight)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Title)
        ? $"[{ProcessName}]（无标题窗口）"
        : $"{Title.Trim()}  [{ProcessName}]";
}

public sealed class WindowsWindowService : IWindowGate
{
    public IReadOnlyList<WindowInfo> ListWindows() => EnumerateWindows()
        .OrderBy(window => window.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();

    public nint FindWindow(string title) => EnumerateWindows()
        .FirstOrDefault(window => string.Equals(window.Title.Trim(), title.Trim(), StringComparison.CurrentCulture))?.Handle ?? 0;

    public nint FindWindow(MacroProfile profile)
    {
        var windows = EnumerateWindows();
        if (!string.IsNullOrWhiteSpace(profile.TargetProcessName))
        {
            var byProcess = windows.Where(window => string.Equals(window.ProcessName, profile.TargetProcessName,
                    StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(window => (long)window.ClientWidth * window.ClientHeight)
                .FirstOrDefault();
            if (byProcess is not null) return byProcess.Handle;
        }
        return windows.FirstOrDefault(window => string.Equals(window.Title.Trim(), profile.TargetWindowTitle.Trim(),
            StringComparison.CurrentCulture))?.Handle ?? 0;
    }

    public ValueTask<bool> IsTargetForegroundAsync(MacroProfile profile, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(IsTargetForeground(profile));
    }

    public bool IsTargetForeground(MacroProfile profile)
    {
        var handle = FindWindow(profile);
        if (handle == 0 || NativeMethods.IsIconic(handle)) return false;
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == 0) return false;
        NativeMethods.GetWindowThreadProcessId(handle, out var targetProcessId);
        NativeMethods.GetWindowThreadProcessId(foreground, out var foregroundProcessId);
        return targetProcessId != 0 && targetProcessId == foregroundProcessId;
    }

    public bool ActivateTarget(MacroProfile profile)
    {
        var handle = FindWindow(profile);
        return handle != 0 && !NativeMethods.IsIconic(handle) && NativeMethods.SetForegroundWindow(handle);
    }

    private static List<WindowInfo> EnumerateWindows()
    {
        List<WindowInfo> windows = [];
        NativeMethods.EnumWindows((handle, _) =>
        {
            if (!NativeMethods.IsWindowVisible(handle) || !NativeMethods.GetClientRect(handle, out var client)) return true;
            var width = client.Right - client.Left;
            var height = client.Bottom - client.Top;
            if (width < 160 || height < 120) return true;
            var buffer = new char[512];
            var length = NativeMethods.GetWindowText(handle, buffer, buffer.Length);
            var title = length > 0 ? new string(buffer, 0, length) : string.Empty;
            NativeMethods.GetWindowThreadProcessId(handle, out var processId);
            string processName;
            try { processName = Process.GetProcessById((int)processId).ProcessName; }
            catch { return true; }
            windows.Add(new(handle, title, processName, width, height));
            return true;
        }, 0);
        return windows;
    }
}
