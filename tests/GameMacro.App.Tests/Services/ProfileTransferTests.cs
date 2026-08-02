using GameMacro.App.Services;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Services;

public sealed class ProfileTransferTests
{
    [Fact]
    public void ImportAsCopy_preserves_configuration_and_creates_new_identity()
    {
        var template = new SkillTemplate
        {
            Signature = [1d, 2d],
            PixelTemplateData = [1, 2, 3, 4],
            PreviewPng = "preview",
            MatchThreshold = .18
        };
        var original = new MacroProfile
        {
            Id = Guid.NewGuid(),
            Name = "五龙",
            TargetProcessName = "ZhuxianClient-Win64-Shipping",
            ToggleHotkey = "F5",
            ScanIntervalMs = 10,
            ShowGameOverlay = false,
            OverlayLeft = .2,
            OverlayTop = .3,
            IconMappings =
            [
                new IconKeyMapping
                {
                    ActionKey = "F2",
                    SkillTemplateId = template.Id
                }
            ]
        };
        var library = new SkillLibrary { Templates = [template] };

        var json = ProfileTransfer.Serialize(original, library);
        var targetLibrary = new SkillLibrary();
        var result = ProfileTransfer.ImportAsCopy(json, targetLibrary);
        var imported = result.Profile;

        Assert.NotEqual(original.Id, imported.Id);
        Assert.Equal("五龙（导入）", imported.Name);
        Assert.Equal(original.TargetProcessName, imported.TargetProcessName);
        Assert.Equal("F5", imported.ToggleHotkey);
        Assert.Equal(10, imported.ScanIntervalMs);
        Assert.False(imported.ShowGameOverlay);
        Assert.Equal(.2, imported.OverlayLeft);
        Assert.Equal(.3, imported.OverlayTop);
        Assert.Equal("F2", imported.IconMappings.Single().ActionKey);
        Assert.Equal(template.Id, imported.IconMappings.Single().SkillTemplateId);
        Assert.Equal([1, 2, 3, 4], targetLibrary.Templates.Single().PixelTemplateData);
        Assert.True(result.LibraryChanged);
    }

    [Fact]
    public void Export_contains_only_templates_used_by_profile()
    {
        var used = new SkillTemplate { Signature = [.1, .2], PreviewPng = "used", PixelTemplateData = [1] };
        var unused = new SkillTemplate { Signature = [.7, .8], PreviewPng = "unused", PixelTemplateData = [2] };
        var profile = new MacroProfile
        {
            IconMappings = [new IconKeyMapping { SkillTemplateId = used.Id, ActionKey = "1" }]
        };

        var json = ProfileTransfer.Serialize(profile, new SkillLibrary { Templates = [used, unused] });
        var package = ProfileTransfer.DeserializePackage(json);

        Assert.Equal(2, package.FormatVersion);
        Assert.Equal(used.Id, Assert.Single(package.Templates).Id);
    }

    [Fact]
    public void ImportAsCopy_accepts_legacy_profile_json_and_migrates_embedded_template()
    {
        var legacy = new MacroProfile
        {
            Version = 2,
            Name = "旧方案",
            IconMappings =
            [
                new IconKeyMapping
                {
                    ActionKey = "4",
                    Signature = [.2, .3],
                    PreviewPng = "legacy",
                    MatchThreshold = .18,
                    PixelTemplateData = [1, 2, 3]
                }
            ]
        };
        var library = new SkillLibrary();

        var result = ProfileTransfer.ImportAsCopy(ProfileTransfer.SerializeLegacy(legacy), library);

        Assert.Equal("旧方案（导入）", result.Profile.Name);
        Assert.NotEqual(Guid.Empty, result.Profile.IconMappings.Single().SkillTemplateId);
        Assert.Single(library.Templates);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("null")]
    public void ImportAsCopy_rejects_invalid_content(string json)
    {
        Assert.Throws<InvalidDataException>(() =>
            ProfileTransfer.ImportAsCopy(json, new SkillLibrary()));
    }
}
