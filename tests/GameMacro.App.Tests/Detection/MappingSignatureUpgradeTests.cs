using GameMacro.App.Detection;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Detection;

public sealed class MappingSignatureUpgradeTests
{
    [Fact]
    public void Rebuilds_legacy_signature_from_saved_png_without_recapture()
    {
        const int size = 48;
        var pixels = Enumerable.Repeat((byte)120, size * size * 4).ToArray();
        for (var index = 3; index < pixels.Length; index += 4) pixels[index] = 255;
        var mapping = new IconKeyMapping
        {
            Signature = new double[66],
            PreviewPng = PngPreviewCodec.EncodeBgra(pixels, size, size),
            MatchThreshold = .18
        };

        var upgraded = MappingSignatureUpgrade.Upgrade(mapping);

        Assert.True(upgraded);
        Assert.True(IconVisualSignature.IsCurrent(mapping.Signature));
        Assert.NotEmpty(mapping.PixelTemplateData);
        Assert.NotNull(PixelIconTemplate.Deserialize(mapping.PixelTemplateData));
    }
}
