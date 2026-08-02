using GameMacro.Core.Models;

namespace GameMacro.App.Platform;

public static class InterruptKeyBindingEditor
{
    public static bool TryAdd(
        ICollection<string> keys,
        string key,
        string toggleHotkey,
        out string? error)
    {
        error = null;
        var canonical = InputKeyOptions.All.FirstOrDefault(option =>
            string.Equals(option, key, StringComparison.OrdinalIgnoreCase));
        if (canonical is null)
        {
            error = $"优先打断键 {key} 不受支持。";
            return false;
        }
        if (string.Equals(canonical, toggleHotkey, StringComparison.OrdinalIgnoreCase))
        {
            error = $"优先打断键 {canonical} 不能与启停热键相同。";
            return false;
        }
        if (!keys.Contains(canonical, StringComparer.OrdinalIgnoreCase)) keys.Add(canonical);
        return true;
    }
}
