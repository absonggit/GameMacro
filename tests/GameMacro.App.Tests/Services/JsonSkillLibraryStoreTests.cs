using GameMacro.App.Services;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Services;

public sealed class JsonSkillLibraryStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), "GameMacroSkillLibraryTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Missing_file_loads_new_library_with_uncategorized_category()
    {
        var store = CreateStore();

        var library = await store.LoadAsync(CancellationToken.None);

        Assert.Contains(library.Categories, category => category.Name == "未分类");
        Assert.Empty(library.Templates);
    }

    [Fact]
    public async Task Save_then_load_preserves_categories_and_templates()
    {
        var store = CreateStore();
        var category = new SkillCategory { Name = "五龙" };
        var library = new SkillLibrary
        {
            Categories = [category],
            Templates =
            [
                new SkillTemplate
                {
                    CategoryId = category.Id,
                    Signature = [.1, .2],
                    PreviewPng = "png",
                    MatchThreshold = .18,
                    PixelTemplateData = [1, 2, 3]
                }
            ]
        };

        await store.SaveAsync(library, CancellationToken.None);
        var restored = await store.LoadAsync(CancellationToken.None);

        Assert.Contains(restored.Categories, item => item.Name == "五龙");
        Assert.Contains(restored.Categories, item => item.Name == "未分类");
        Assert.Equal([1, 2, 3], restored.Templates.Single().PixelTemplateData);
    }

    [Fact]
    public async Task Second_save_creates_backup_and_corrupt_main_recovers_from_it()
    {
        var store = CreateStore();
        var library = new SkillLibrary
        {
            Categories = [new SkillCategory { Name = "第一次" }]
        };
        await store.SaveAsync(library, CancellationToken.None);
        library.Categories[0].Name = "第二次";
        await store.SaveAsync(library, CancellationToken.None);
        await File.WriteAllTextAsync(store.Path, "{broken");

        var restored = await store.LoadAsync(CancellationToken.None);

        Assert.Contains(restored.Categories, item => item.Name == "第一次");
        Assert.True(File.Exists(store.CorruptPath));
        Assert.True(File.Exists(store.BackupPath));
    }

    [Fact]
    public async Task Corrupt_main_and_backup_throws_without_overwriting_main()
    {
        var store = CreateStore();
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(store.Path, "{main-broken");
        await File.WriteAllTextAsync(store.BackupPath, "{backup-broken");

        await Assert.ThrowsAsync<InvalidDataException>(
            () => store.LoadAsync(CancellationToken.None));

        Assert.Equal("{main-broken", await File.ReadAllTextAsync(store.Path));
        Assert.True(File.Exists(store.CorruptPath));
    }

    private JsonSkillLibraryStore CreateStore()
        => new(Path.Combine(_directory, "SkillLibrary.json"));

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }
}
