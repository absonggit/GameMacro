namespace GameMacro.App.Platform;

public static class VirtualKeyParser
{
    public static ushort? Parse(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        key = key.Trim().ToUpperInvariant();
        if (key.StartsWith('F') && int.TryParse(key[1..], out var function) && function is >= 1 and <= 24)
            return (ushort)(0x70 + function - 1);
        if (key == "~") return 0xC0;
        if (key.Length == 1 && char.IsLetterOrDigit(key[0])) return key[0];
        return null;
    }
}
