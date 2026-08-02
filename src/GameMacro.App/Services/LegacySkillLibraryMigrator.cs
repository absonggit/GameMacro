using GameMacro.Core.Models;

namespace GameMacro.App.Services;

public sealed record MigrationResult(
    bool Changed,
    int AddedTemplates,
    int ReusedTemplates);

public static class LegacySkillLibraryMigrator
{
    public static MigrationResult Migrate(MacroProfile profile, SkillLibrary library)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(library);
        var catalog = new SkillLibraryCatalog(library);
        var uncategorized = catalog.EnsureUncategorized();
        var changed = profile.Version < 3;
        var added = 0;
        var reused = 0;
        foreach (var mapping in profile.IconMappings)
        {
            if (mapping.SkillTemplateId != Guid.Empty) continue;
            if (!mapping.IsCalibrated || mapping.PixelTemplateData.Length == 0) continue;
            var candidate = new SkillTemplate
            {
                CategoryId = uncategorized.Id,
                Signature = mapping.Signature.ToArray(),
                PreviewPng = mapping.PreviewPng,
                MatchThreshold = mapping.MatchThreshold,
                PixelTemplateData = mapping.PixelTemplateData.ToArray()
            };
            var existing = catalog.FindDuplicate(candidate);
            if (existing is null)
            {
                library.Templates.Add(candidate);
                existing = candidate;
                added++;
            }
            else
            {
                reused++;
            }
            mapping.SkillTemplateId = existing.Id;
            mapping.Signature = [];
            mapping.PreviewPng = string.Empty;
            mapping.MatchThreshold = 0;
            mapping.PixelTemplateData = [];
            changed = true;
        }
        if (profile.Version != 3)
        {
            profile.Version = 3;
            changed = true;
        }
        return new MigrationResult(changed, added, reused);
    }
}
