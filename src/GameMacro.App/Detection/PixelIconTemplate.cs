namespace GameMacro.App.Detection;

public sealed record PixelIconTemplate(int Version, byte[] Rgb)
{
    public const int CurrentVersion = 1;
    public const int Size = 32;
    private const int PixelBytes = Size * Size * 3;

    public byte[] Serialize()
    {
        if (Version != CurrentVersion || Rgb.Length != PixelBytes)
            throw new InvalidOperationException("像素模板格式无效。");
        var data = new byte[PixelBytes + 5];
        data[0] = (byte)'P';
        data[1] = (byte)'I';
        data[2] = (byte)'X';
        data[3] = (byte)Version;
        data[4] = Size;
        Rgb.CopyTo(data, 5);
        return data;
    }

    public static PixelIconTemplate? Deserialize(byte[]? data)
    {
        if (data is not { Length: PixelBytes + 5 }
            || data[0] != (byte)'P' || data[1] != (byte)'I' || data[2] != (byte)'X'
            || data[3] != CurrentVersion || data[4] != Size)
            return null;
        return new(CurrentVersion, data[5..]);
    }
}
