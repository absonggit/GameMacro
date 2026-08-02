using System.IO;
using GameMacro.App.Detection;
using GameMacro.Core.Models;

namespace GameMacro.App.Services;

public sealed record SkillLibraryStartupResult(
    SkillLibrary Library,
    int MigratedProfiles,
    int AddedTemplates);

public static class SkillLibraryStartup
{
    public static async Task<SkillLibraryStartupResult> LoadAndMigrateAsync(
        JsonSkillLibraryStore libraryStore,
        IReadOnlyCollection<MacroProfile> profiles,
        JsonProfileStore profileStore,
        CancellationToken cancellationToken)
    {
        var libraryFileExisted = File.Exists(libraryStore.Path);
        var library = await libraryStore.LoadAsync(cancellationToken);
        List<MacroProfile> changedProfiles = [];
        var addedTemplates = 0;
        foreach (var profile in profiles)
        {
            MappingSignatureUpgrade.UpgradeAll(profile.IconMappings);
            var result = LegacySkillLibraryMigrator.Migrate(profile, library);
            addedTemplates += result.AddedTemplates;
            if (result.Changed) changedProfiles.Add(profile);
        }
        if (!libraryFileExisted || changedProfiles.Count > 0 || addedTemplates > 0)
            await libraryStore.SaveAsync(library, cancellationToken);
        foreach (var profile in changedProfiles)
            await profileStore.SaveAsync(profile, cancellationToken);
        return new SkillLibraryStartupResult(library, changedProfiles.Count, addedTemplates);
    }
}
