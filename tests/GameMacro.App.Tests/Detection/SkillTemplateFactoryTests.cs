using GameMacro.App.Detection;

namespace GameMacro.App.Tests.Detection;

public sealed class SkillTemplateFactoryTests
{
    [Fact]
    public void FromCapturedRegion_preserves_visual_data_and_category()
    {
        var categoryId = Guid.NewGuid();
        var pixels = Pixels(24, 24);
        var captured = new CapturedRegion(
            pixels, 24, 24, IconVisualSignature.Create(pixels, 24, 24), "preview");

        var template = SkillTemplateFactory.FromCapturedRegion(captured, categoryId);

        Assert.Equal(categoryId, template.CategoryId);
        Assert.Equal("preview", template.PreviewPng);
        Assert.NotEmpty(template.Signature);
        Assert.NotEmpty(template.PixelTemplateData);
        Assert.Equal(.18, template.MatchThreshold);
    }

    [Fact]
    public void FromDetectedIcons_builds_one_template_per_icon()
    {
        var categoryId = Guid.NewGuid();
        var pixels = Pixels(24, 24);
        var icon = new DetectedSkillIcon(
            new PixelRegion(0, 0, 24, 24), pixels, 24, 24, "icon",
            IconVisualSignature.Create(pixels, 24, 24), .17);

        var templates = SkillTemplateFactory.FromDetectedIcons([icon], categoryId);

        var template = Assert.Single(templates);
        Assert.Equal(categoryId, template.CategoryId);
        Assert.Equal(.17, template.MatchThreshold);
        Assert.NotEmpty(template.PixelTemplateData);
    }

    private static byte[] Pixels(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var index = 0; index < width * height; index++)
        {
            pixels[index * 4] = (byte)(index % 255);
            pixels[index * 4 + 1] = (byte)((index * 2) % 255);
            pixels[index * 4 + 2] = (byte)((index * 3) % 255);
            pixels[index * 4 + 3] = 255;
        }
        return pixels;
    }
}
