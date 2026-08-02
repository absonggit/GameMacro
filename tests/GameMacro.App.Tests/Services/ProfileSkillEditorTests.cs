using GameMacro.App.Services;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Services;

public sealed class ProfileSkillEditorTests
{
    private readonly ProfileSkillEditor _editor = new();

    [Fact]
    public void Different_templates_may_share_one_action_key()
    {
        var profile = new MacroProfile();
        var first = _editor.AddTemplate(profile, Guid.NewGuid()).Mapping;
        var second = _editor.AddTemplate(profile, Guid.NewGuid()).Mapping;

        _editor.AssignKey(profile, first.Id, "4");
        _editor.AssignKey(profile, second.Id, "4");

        Assert.All(profile.IconMappings, mapping => Assert.Equal("4", mapping.ActionKey));
    }

    [Fact]
    public void Adding_same_template_twice_returns_existing_mapping()
    {
        var profile = new MacroProfile();
        var templateId = Guid.NewGuid();

        var first = _editor.AddTemplate(profile, templateId);
        var second = _editor.AddTemplate(profile, templateId);

        Assert.True(first.Added);
        Assert.False(second.Added);
        Assert.Equal(first.Mapping.Id, second.Mapping.Id);
        Assert.Single(profile.IconMappings);
    }

    [Fact]
    public void New_mapping_stores_only_template_reference_and_unassigned_key()
    {
        var profile = new MacroProfile();
        var templateId = Guid.NewGuid();

        var result = _editor.AddTemplate(profile, templateId);

        Assert.Equal(templateId, result.Mapping.SkillTemplateId);
        Assert.Equal("点击设置", result.Mapping.ActionKey);
        Assert.True(result.Mapping.Enabled);
        Assert.Empty(result.Mapping.Signature);
        Assert.Empty(result.Mapping.PixelTemplateData);
    }

    [Fact]
    public void AssignKey_rejects_unsupported_key_and_remove_only_changes_profile()
    {
        var profile = new MacroProfile();
        var mapping = _editor.AddTemplate(profile, Guid.NewGuid()).Mapping;

        Assert.Throws<InvalidOperationException>(() =>
            _editor.AssignKey(profile, mapping.Id, "Space"));
        Assert.True(_editor.RemoveMapping(profile, mapping.Id));
        Assert.Empty(profile.IconMappings);
    }
}
