using System.Windows.Media;
using System.Windows.Media.Imaging;
using GameMacro.Core.Models;

namespace GameMacro.App.Detection;

public static class MappingSignatureUpgrade
{
    public static bool Upgrade(IconKeyMapping mapping)
    {
        var signatureCurrent = IconVisualSignature.IsCurrent(mapping.Signature);
        var pixelTemplateCurrent = PixelIconTemplate.Deserialize(mapping.PixelTemplateData) is not null;
        if (signatureCurrent && pixelTemplateCurrent) return false;
        var source = PngPreviewCodec.Decode(mapping.PreviewPng);
        if (source is null) return false;
        var bitmap = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        if (!signatureCurrent)
            mapping.Signature = IconTemplateNormalizer.CreateSignature(pixels, bitmap.PixelWidth, bitmap.PixelHeight);
        if (!pixelTemplateCurrent)
            mapping.PixelTemplateData = PixelIconTemplateBuilder.Create(pixels, bitmap.PixelWidth, bitmap.PixelHeight).Serialize();
        return true;
    }

    public static int UpgradeAll(IEnumerable<IconKeyMapping> mappings)
        => mappings.Count(Upgrade);
}
