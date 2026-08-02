using GameMacro.Core.Models;

namespace GameMacro.App.Services;

public sealed record SkillMappingResolution(
    IReadOnlyList<IconKeyMapping> Mappings,
    IReadOnlyList<Guid> MissingTemplateIds);

public static class SkillMappingResolver
{
    public static SkillMappingResolution Resolve(MacroProfile profile, SkillLibrary library)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(library);
        var templates = library.Templates.ToDictionary(template => template.Id);
        List<IconKeyMapping> mappings = [];
        List<Guid> missing = [];
        foreach (var source in profile.IconMappings)
        {
            if (!templates.TryGetValue(source.SkillTemplateId, out var template))
            {
                if (!missing.Contains(source.SkillTemplateId)) missing.Add(source.SkillTemplateId);
                continue;
            }
            mappings.Add(new IconKeyMapping
            {
                Id = source.Id,
                SkillTemplateId = template.Id,
                Enabled = source.Enabled,
                ActionKey = source.ActionKey,
                Signature = template.Signature.ToArray(),
                PreviewPng = template.PreviewPng,
                MatchThreshold = template.MatchThreshold,
                PixelTemplateData = template.PixelTemplateData.ToArray()
            });
        }
        return new SkillMappingResolution(mappings, missing);
    }
}
