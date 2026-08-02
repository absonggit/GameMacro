using GameMacro.App.Services;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Services;

public sealed class SkillLibraryStartupTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), "GameMacroStartupTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task LoadAndMigrate_merges_duplicate_legacy_templates_and_saves_profiles()
    {
        var profileStore = new JsonProfileStore(Path.Combine(_directory, "Profiles"));
        var libraryStore = new JsonSkillLibraryStore(Path.Combine(_directory, "SkillLibrary.json"));
        var first = LegacyProfile("一", "F3");
        var second = LegacyProfile("二", "1");
        await profileStore.SaveAsync(first, CancellationToken.None);
        await profileStore.SaveAsync(second, CancellationToken.None);

        var result = await SkillLibraryStartup.LoadAndMigrateAsync(
            libraryStore, [first, second], profileStore, CancellationToken.None);

        Assert.Equal(2, result.MigratedProfiles);
        Assert.Single(result.Library.Templates);
        Assert.Equal(first.IconMappings.Single().SkillTemplateId, second.IconMappings.Single().SkillTemplateId);
        var restored = await profileStore.LoadAllAsync(CancellationToken.None);
        Assert.All(restored, profile => Assert.Equal(3, profile.Version));
    }

    [Fact]
    public async Task Second_start_is_idempotent()
    {
        var profileStore = new JsonProfileStore(Path.Combine(_directory, "Profiles"));
        var libraryStore = new JsonSkillLibraryStore(Path.Combine(_directory, "SkillLibrary.json"));
        var profile = LegacyProfile("一", "F3");

        await SkillLibraryStartup.LoadAndMigrateAsync(
            libraryStore, [profile], profileStore, CancellationToken.None);
        var second = await SkillLibraryStartup.LoadAndMigrateAsync(
            libraryStore, [profile], profileStore, CancellationToken.None);

        Assert.Equal(0, second.MigratedProfiles);
        Assert.Single(second.Library.Templates);
    }

    private static MacroProfile LegacyProfile(string name, string key) => new()
    {
        Version = 2,
        Name = name,
        IconMappings =
        [
            new IconKeyMapping
            {
                ActionKey = key,
                Signature = [.1, .2],
                PreviewPng = "png",
                MatchThreshold = .18,
                PixelTemplateData = [1, 2, 3]
            }
        ]
    };

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }
}
