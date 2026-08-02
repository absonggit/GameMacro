using GameMacro.App.Detection;
using GameMacro.Core.Models;

namespace GameMacro.App.Services;

public sealed record SkillTemplateAddResult(
    IReadOnlyList<SkillTemplate> Added,
    IReadOnlyList<SkillTemplate> Reused);

public sealed record TemplateDeleteCheck(
    bool Allowed,
    IReadOnlyList<string> ReferencingProfiles);

public sealed class SkillLibraryCatalog(SkillLibrary library)
{
    public const string UncategorizedName = "未分类";
    public const double DuplicateDistance = .06;

    public SkillCategory EnsureUncategorized()
    {
        var category = library.Categories.FirstOrDefault(item =>
            string.Equals(item.Name, UncategorizedName, StringComparison.OrdinalIgnoreCase));
        if (category is not null) return category;
        category = new SkillCategory { Name = UncategorizedName };
        library.Categories.Add(category);
        return category;
    }

    public SkillCategory CreateCategory(string name)
    {
        name = NormalizeCategoryName(name);
        if (library.Categories.Any(item =>
                string.Equals(item.Name, name, StringComparison.CurrentCultureIgnoreCase)))
            throw new InvalidOperationException($"职业分类“{name}”已经存在。");
        var category = new SkillCategory { Name = name };
        library.Categories.Add(category);
        return category;
    }

    public void RenameCategory(Guid categoryId, string name)
    {
        var category = FindCategory(categoryId);
        name = NormalizeCategoryName(name);
        if (library.Categories.Any(item => item.Id != categoryId &&
                string.Equals(item.Name, name, StringComparison.CurrentCultureIgnoreCase)))
            throw new InvalidOperationException($"职业分类“{name}”已经存在。");
        category.Name = name;
    }

    public void DeleteCategory(Guid categoryId)
    {
        var category = FindCategory(categoryId);
        if (library.Templates.Any(template => template.CategoryId == categoryId))
            throw new InvalidOperationException("该职业分类中仍有技能模板，不能删除。");
        if (string.Equals(category.Name, UncategorizedName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("默认的“未分类”不能删除。");
        library.Categories.Remove(category);
    }

    public SkillTemplate? FindDuplicate(SkillTemplate candidate)
        => library.Templates.FirstOrDefault(existing =>
            existing.Signature.Length > 0
            && candidate.Signature.Length > 0
            && IconVisualSignature.Distance(existing.Signature, candidate.Signature) <= DuplicateDistance);

    public SkillTemplateAddResult AddTemplates(Guid categoryId, IEnumerable<SkillTemplate> templates)
    {
        _ = FindCategory(categoryId);
        List<SkillTemplate> added = [];
        List<SkillTemplate> reused = [];
        foreach (var template in templates)
        {
            var duplicate = FindDuplicate(template);
            if (duplicate is not null)
            {
                reused.Add(duplicate);
                continue;
            }
            template.CategoryId = categoryId;
            if (template.Id == Guid.Empty) template.Id = Guid.NewGuid();
            library.Templates.Add(template);
            added.Add(template);
        }
        return new SkillTemplateAddResult(added, reused);
    }

    public TemplateDeleteCheck CanDeleteTemplate(Guid templateId, IEnumerable<MacroProfile> profiles)
    {
        var names = profiles
            .Where(profile => profile.IconMappings.Any(mapping => mapping.SkillTemplateId == templateId))
            .Select(profile => profile.Name)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return new TemplateDeleteCheck(names.Count == 0, names);
    }

    public void DeleteTemplate(Guid templateId, IEnumerable<MacroProfile> profiles)
    {
        var check = CanDeleteTemplate(templateId, profiles);
        if (!check.Allowed)
            throw new InvalidOperationException(
                $"该技能模板正在被以下方案使用：{string.Join("、", check.ReferencingProfiles)}");
        var template = library.Templates.FirstOrDefault(item => item.Id == templateId)
            ?? throw new KeyNotFoundException("技能模板不存在。");
        library.Templates.Remove(template);
    }

    private SkillCategory FindCategory(Guid categoryId)
        => library.Categories.FirstOrDefault(item => item.Id == categoryId)
           ?? throw new KeyNotFoundException("职业分类不存在。");

    private static string NormalizeCategoryName(string name)
    {
        name = name?.Trim() ?? string.Empty;
        return name.Length == 0
            ? throw new InvalidOperationException("职业分类名称不能为空。")
            : name;
    }
}
