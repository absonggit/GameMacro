using GameMacro.App.Detection;
using GameMacro.Core.Models;

namespace GameMacro.App.Services;

public static class SkillLibraryPackageMerger
{
    public static SkillLibraryMergeResult Merge(ProfileExportPackage package, SkillLibrary library)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(library);
        var changed = false;
        var idMap = new Dictionary<Guid, Guid>();
        foreach (var imported in package.Templates)
        {
            var sameId = library.Templates.FirstOrDefault(item => item.Id == imported.Id);
            if (sameId is not null && SameVisual(sameId, imported))
            {
                idMap[imported.Id] = sameId.Id;
                continue;
            }
            var duplicate = library.Templates.FirstOrDefault(item =>
                item.Signature.Length > 0
                && imported.Signature.Length > 0
                && IconVisualSignature.Distance(item.Signature, imported.Signature)
                    <= SkillLibraryCatalog.DuplicateDistance);
            if (duplicate is not null)
            {
                idMap[imported.Id] = duplicate.Id;
                continue;
            }
            var categoryId = MergeCategory(imported.CategoryId, package.Categories, library, ref changed);
            var clone = Clone(imported);
            if (sameId is not null || clone.Id == Guid.Empty) clone.Id = Guid.NewGuid();
            clone.CategoryId = categoryId;
            library.Templates.Add(clone);
            idMap[imported.Id] = clone.Id;
            changed = true;
        }

        foreach (var mapping in package.Profile.IconMappings)
        {
            if (idMap.TryGetValue(mapping.SkillTemplateId, out var replacement))
                mapping.SkillTemplateId = replacement;
        }
        return new SkillLibraryMergeResult(package.Profile, changed);
    }

    private static Guid MergeCategory(
        Guid importedCategoryId,
        IReadOnlyCollection<SkillCategory> importedCategories,
        SkillLibrary library,
        ref bool changed)
    {
        var imported = importedCategories.FirstOrDefault(item => item.Id == importedCategoryId);
        if (imported is null) return new SkillLibraryCatalog(library).EnsureUncategorized().Id;
        var sameId = library.Categories.FirstOrDefault(item => item.Id == imported.Id);
        if (sameId is not null && string.Equals(sameId.Name, imported.Name, StringComparison.CurrentCulture))
            return sameId.Id;
        var sameName = library.Categories.FirstOrDefault(item =>
            string.Equals(item.Name, imported.Name, StringComparison.CurrentCultureIgnoreCase));
        if (sameName is not null) return sameName.Id;
        var category = new SkillCategory
        {
            Id = sameId is null && imported.Id != Guid.Empty ? imported.Id : Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(imported.Name) ? SkillLibraryCatalog.UncategorizedName : imported.Name.Trim()
        };
        library.Categories.Add(category);
        changed = true;
        return category.Id;
    }

    private static bool SameVisual(SkillTemplate left, SkillTemplate right)
        => left.Signature.SequenceEqual(right.Signature)
           && left.PixelTemplateData.SequenceEqual(right.PixelTemplateData)
           && string.Equals(left.PreviewPng, right.PreviewPng, StringComparison.Ordinal)
           && Math.Abs(left.MatchThreshold - right.MatchThreshold) < .000001;

    private static SkillTemplate Clone(SkillTemplate source) => new()
    {
        Id = source.Id,
        CategoryId = source.CategoryId,
        Signature = source.Signature.ToArray(),
        PreviewPng = source.PreviewPng,
        MatchThreshold = source.MatchThreshold,
        PixelTemplateData = source.PixelTemplateData.ToArray()
    };
}
