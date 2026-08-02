using GameMacro.App.Services;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Services;

public sealed class LegacySkillLibraryMigratorTests
{
    [Fact]
    public void Migrate_reuses_duplicate_template_and_preserves_mapping_key()
    {
        var existing = Template([.1, .2]);
        var library = new SkillLibrary { Templates = [existing] };
        var profile = LegacyProfile("F3", [.1, .2]);

        var result = LegacySkillLibraryMigrator.Migrate(profile, library);

        Assert.True(result.Changed);
        Assert.Equal(0, result.AddedTemplates);
        Assert.Equal(1, result.ReusedTemplates);
        var mapping = Assert.Single(profile.IconMappings);
        Assert.Equal(existing.Id, mapping.SkillTemplateId);
        Assert.Equal("F3", mapping.ActionKey);
        Assert.Empty(mapping.Signature);
        Assert.Empty(mapping.PixelTemplateData);
        Assert.Equal(3, profile.Version);
        Assert.Single(library.Templates);
    }

    [Fact]
    public void Migrate_adds_distinct_templates_to_uncategorized()
    {
        var library = new SkillLibrary();
        var profile = LegacyProfile("4", [.7, .8]);

        var result = LegacySkillLibraryMigrator.Migrate(profile, library);

        Assert.Equal(1, result.AddedTemplates);
        var template = Assert.Single(library.Templates);
        Assert.Equal(profile.IconMappings.Single().SkillTemplateId, template.Id);
        Assert.Equal("未分类", library.Categories.Single(category => category.Id == template.CategoryId).Name);
    }

    [Fact]
    public void Migration_is_idempotent()
    {
        var library = new SkillLibrary();
        var profile = LegacyProfile("1", [.3, .4]);

        LegacySkillLibraryMigrator.Migrate(profile, library);
        var second = LegacySkillLibraryMigrator.Migrate(profile, library);

        Assert.False(second.Changed);
        Assert.Single(library.Templates);
    }

    private static MacroProfile LegacyProfile(string key, double[] signature) => new()
    {
        Version = 2,
        IconMappings =
        [
            new IconKeyMapping
            {
                ActionKey = key,
                Signature = signature,
                PreviewPng = "legacy-png",
                MatchThreshold = .18,
                PixelTemplateData = [1, 2, 3, 4]
            }
        ]
    };

    private static SkillTemplate Template(double[] signature) => new()
    {
        Signature = signature,
        PreviewPng = "existing-png",
        MatchThreshold = .18,
        PixelTemplateData = [5, 6, 7, 8]
    };
}
