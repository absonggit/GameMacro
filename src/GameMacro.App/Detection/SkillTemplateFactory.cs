using GameMacro.Core.Models;

namespace GameMacro.App.Detection;

public static class SkillTemplateFactory
{
    public static SkillTemplate FromCapturedRegion(CapturedRegion captured, Guid categoryId)
        => new()
        {
            CategoryId = categoryId,
            PreviewPng = captured.PreviewPng,
            Signature = captured.Signature.ToArray(),
            MatchThreshold = .18,
            PixelTemplateData = PixelIconTemplateBuilder
                .Create(captured.Pixels, captured.Width, captured.Height)
                .Serialize()
        };

    public static List<SkillTemplate> FromDetectedIcons(
        IEnumerable<DetectedSkillIcon> icons,
        Guid categoryId)
        => icons.Select(icon => new SkillTemplate
        {
            CategoryId = categoryId,
            PreviewPng = icon.PreviewPng,
            Signature = icon.Signature.ToArray(),
            MatchThreshold = icon.MatchThreshold,
            PixelTemplateData = PixelIconTemplateBuilder
                .Create(icon.Pixels, icon.Width, icon.Height)
                .Serialize()
        }).ToList();
}
