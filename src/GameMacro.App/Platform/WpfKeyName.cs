using System.Windows.Input;

namespace GameMacro.App.Platform;

public static class WpfKeyName
{
    public static string? FromKey(Key key)
    {
        if (key is >= Key.F1 and <= Key.F12) return $"F{key - Key.F1 + 1}";
        if (key is >= Key.D0 and <= Key.D9) return $"{key - Key.D0}";
        if (key is >= Key.NumPad0 and <= Key.NumPad9) return $"{key - Key.NumPad0}";
        if (key is >= Key.A and <= Key.Z) return ((char)('A' + key - Key.A)).ToString();
        return key == Key.Oem3 ? "~" : null;
    }
}
