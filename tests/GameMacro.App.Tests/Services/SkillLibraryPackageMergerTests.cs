using GameMacro.App.Services;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Services;

public sealed class SkillLibraryPackageMergerTests
{
    [Fact]
    public void Merge_rewrites_reference_when_same_id_has_different_content()
    {
        var sharedId = Guid.NewGuid();
        var local = Template(sharedId, [.1, .2], "local");
        var imported = Template(sharedId, [.8, .9], "imported");
        var profile = new MacroProfile
        {
            IconMappings = [new IconKeyMapping { SkillTemplateId = sharedId, ActionKey = "F3" }]
        };
        var package = new ProfileExportPackage { Profile = profile, Templates = [imported] };
        var library = new SkillLibrary { Templates = [local] };

        var result = SkillLibraryPackageMerger.Merge(package, library);

        var rewrittenId = result.Profile.IconMappings.Single().SkillTemplateId;
        Assert.NotEqual(sharedId, rewrittenId);
        Assert.Equal(2, library.Templates.Count);
        Assert.Contains(library.Templates, item => item.Id == rewrittenId && item.PreviewPng == "imported");
    }

    [Fact]
    public void Merge_reuses_visual_duplicate_with_different_id()
    {
        var local = Template(Guid.NewGuid(), [.1, .2], "local");
        var imported = Template(Guid.NewGuid(), [.1, .2], "imported");
        var profile = new MacroProfile
        {
            IconMappings = [new IconKeyMapping { SkillTemplateId = imported.Id, ActionKey = "2" }]
        };
        var library = new SkillLibrary { Templates = [local] };

        var result = SkillLibraryPackageMerger.Merge(
            new ProfileExportPackage { Profile = profile, Templates = [imported] }, library);

        Assert.Equal(local.Id, result.Profile.IconMappings.Single().SkillTemplateId);
        Assert.Single(library.Templates);
    }

    private static SkillTemplate Template(Guid id, double[] signature, string preview) => new()
    {
        Id = id,
        Signature = signature,
        PreviewPng = preview,
        MatchThreshold = .18,
        PixelTemplateData = [1, 2, 3]
    };
}
