using GameMacro.App.Detection;

namespace GameMacro.App.Tests.Detection;

public sealed class PngPreviewCodecTests
{
    [Fact]
    public void Encodes_and_decodes_bgra_preview()
    {
        byte[] pixels =
        [
            0, 0, 255, 255, 0, 255, 0, 255,
            255, 0, 0, 255, 255, 255, 255, 255
        ];

        var encoded = PngPreviewCodec.EncodeBgra(pixels, 2, 2);
        var image = PngPreviewCodec.Decode(encoded);

        Assert.NotEmpty(encoded);
        Assert.NotNull(image);
        Assert.Equal(2, image.PixelWidth);
        Assert.Equal(2, image.PixelHeight);
        Assert.True(image.IsFrozen);
    }
}
