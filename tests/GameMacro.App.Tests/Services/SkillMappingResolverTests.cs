using GameMacro.App.Services;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Services;

public sealed class SkillMappingResolverTests
{
    [Fact]
    public void Resolve_combines_template_visual_data_with_profile_key()
    {
        var template = new SkillTemplate
        {
            Signature = [.1, .2],
            PreviewPng = "png",
            MatchThreshold = .15,
            PixelTemplateData = [1, 2, 3, 4]
        };
        var profile = new MacroProfile
        {
            IconMappings =
            [
                new IconKeyMapping
                {
                    SkillTemplateId = template.Id,
                    ActionKey = "F3",
                    Enabled = true
                }
            ]
        };
        var library = new SkillLibrary { Templates = [template] };

        var result = SkillMappingResolver.Resolve(profile, library);

        var resolved = Assert.Single(result.Mappings);
        Assert.Equal("F3", resolved.ActionKey);
        Assert.True(resolved.Enabled);
        Assert.Equal(template.Id, resolved.SkillTemplateId);
        Assert.Equal(template.Signature, resolved.Signature);
        Assert.Equal(template.PixelTemplateData, resolved.PixelTemplateData);
        Assert.Empty(result.MissingTemplateIds);
    }

    [Fact]
    public void Resolve_reports_missing_template_without_creating_mapping()
    {
        var missingId = Guid.NewGuid();
        var profile = new MacroProfile
        {
            IconMappings = [new IconKeyMapping { SkillTemplateId = missingId, ActionKey = "1" }]
        };

        var result = SkillMappingResolver.Resolve(profile, new SkillLibrary());

        Assert.Empty(result.Mappings);
        Assert.Equal([missingId], result.MissingTemplateIds);
    }
}
