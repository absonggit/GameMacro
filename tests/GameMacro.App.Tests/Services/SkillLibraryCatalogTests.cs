using GameMacro.App.Services;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Services;

public sealed class SkillLibraryCatalogTests
{
    [Fact]
    public void AddTemplates_deduplicates_identical_visuals_but_keeps_distinct_variants()
    {
        var library = new SkillLibrary();
        var catalog = new SkillLibraryCatalog(library);
        var category = catalog.EnsureUncategorized();

        var result = catalog.AddTemplates(category.Id,
        [
            Template([.1, .2]),
            Template([.1, .2]),
            Template([.8, .7])
        ]);

        Assert.Equal(2, result.Added.Count);
        Assert.Single(result.Reused);
        Assert.Equal(2, library.Templates.Count);
        Assert.All(library.Templates, template => Assert.Equal(category.Id, template.CategoryId));
    }

    [Fact]
    public void EnsureUncategorized_is_idempotent()
    {
        var catalog = new SkillLibraryCatalog(new SkillLibrary());

        var first = catalog.EnsureUncategorized();
        var second = catalog.EnsureUncategorized();

        Assert.Equal(first.Id, second.Id);
        Assert.Equal("未分类", first.Name);
    }

    [Fact]
    public void Referenced_template_cannot_be_deleted_and_reports_profile()
    {
        var library = new SkillLibrary();
        var catalog = new SkillLibraryCatalog(library);
        var category = catalog.EnsureUncategorized();
        var template = Template([.1, .2]);
        template.CategoryId = category.Id;
        library.Templates.Add(template);
        var profile = new MacroProfile
        {
            Name = "爆发方案",
            IconMappings = [new IconKeyMapping { SkillTemplateId = template.Id }]
        };

        var result = catalog.CanDeleteTemplate(template.Id, [profile]);

        Assert.False(result.Allowed);
        Assert.Contains("爆发方案", result.ReferencingProfiles);
        Assert.Throws<InvalidOperationException>(() => catalog.DeleteTemplate(template.Id, [profile]));
        Assert.Single(library.Templates);
    }

    [Fact]
    public void Category_with_templates_cannot_be_deleted()
    {
        var library = new SkillLibrary();
        var catalog = new SkillLibraryCatalog(library);
        var category = catalog.CreateCategory("律令");
        var template = Template([.1, .2]);
        template.CategoryId = category.Id;
        library.Templates.Add(template);

        Assert.Throws<InvalidOperationException>(() => catalog.DeleteCategory(category.Id));
    }

    private static SkillTemplate Template(double[] signature) => new()
    {
        Signature = signature,
        PreviewPng = Convert.ToBase64String([1, 2, 3]),
        MatchThreshold = .18,
        PixelTemplateData = [1, 2, 3, 4]
    };
}
