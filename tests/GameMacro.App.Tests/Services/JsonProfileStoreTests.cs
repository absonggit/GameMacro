using GameMacro.App.Services;
using GameMacro.Core.Models;
using System.Text.Json;

namespace GameMacro.App.Tests.Services;

public sealed class JsonProfileStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "GameMacroTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Save_then_load_preserves_profile()
    {
        var store = new JsonProfileStore(_directory);
        var profile = new MacroProfile
        {
            Name = "测试方案",
            Rules = [new MacroRule { ActionKey = "F4" }]
        };

        await store.SaveAsync(profile, CancellationToken.None);
        var loaded = await store.LoadAllAsync(CancellationToken.None);

        Assert.Equal("测试方案", loaded.Single().Name);
        Assert.Equal("F4", loaded.Single().Rules.Single().ActionKey);
    }

    [Fact]
    public async Task Saving_twice_creates_valid_backup()
    {
        var store = new JsonProfileStore(_directory);
        var profile = new MacroProfile { Name = "初始名称" };
        await store.SaveAsync(profile, CancellationToken.None);
        profile.Name = "修改名称";

        await store.SaveAsync(profile, CancellationToken.None);

        Assert.True(File.Exists(store.GetBackupPath(profile.Id)));
        using var backup = JsonDocument.Parse(await File.ReadAllTextAsync(store.GetBackupPath(profile.Id)));
        Assert.Equal("初始名称", backup.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Save_then_load_preserves_dynamic_icon_configuration()
    {
        var store = new JsonProfileStore(_directory);
        var profile = new MacroProfile
        {
            Version = 2,
            DetectionX = .6,
            DetectionY = .5,
            DetectionWidth = .04,
            DetectionHeight = .05,
            DetectionPreviewPng = "region-png",
            ScanIntervalMs = 25,
            ShowGameOverlay = false,
            OverlayLeft = .25,
            OverlayTop = .35,
            InterruptKeys = ["Q", "E"],
            IconMappings =
            [
                new IconKeyMapping
                {
                    ActionKey = "F4",
                    Signature = [1d, 2d],
                    PreviewPng = "icon-png",
                    MatchThreshold = .12,
                    PixelTemplateData = [1, 2, 3, 4]
                }
            ]
        };

        await store.SaveAsync(profile, CancellationToken.None);
        var restored = (await store.LoadAllAsync(CancellationToken.None)).Single();

        Assert.Equal("region-png", restored.DetectionPreviewPng);
        Assert.Equal(25, restored.ScanIntervalMs);
        Assert.False(restored.ShowGameOverlay);
        Assert.Equal(.25, restored.OverlayLeft);
        Assert.Equal(.35, restored.OverlayTop);
        Assert.Equal(["Q", "E"], restored.InterruptKeys);
        Assert.Equal("F4", restored.IconMappings.Single().ActionKey);
        Assert.Equal([1d, 2d], restored.IconMappings.Single().Signature);
        Assert.Equal([1, 2, 3, 4], restored.IconMappings.Single().PixelTemplateData);
    }

    [Fact]
    public async Task Loading_legacy_profile_preserves_file_and_returns_empty_version_two_editor()
    {
        Directory.CreateDirectory(_directory);
        var id = Guid.NewGuid();
        var path = Path.Combine(_directory, $"{id:N}.json");
        const string legacyJson = """{"version":1,"name":"旧方案","targetWindowTitle":"游戏","toggleHotkey":"F7","rules":[{"actionKey":"F1"}]}""";
        await File.WriteAllTextAsync(path, legacyJson);
        var store = new JsonProfileStore(_directory);

        var loaded = (await store.LoadAllAsync(CancellationToken.None)).Single();

        Assert.Equal(2, loaded.Version);
        Assert.Equal("旧方案", loaded.Name);
        Assert.Equal("游戏", loaded.TargetWindowTitle);
        Assert.Equal("F7", loaded.ToggleHotkey);
        Assert.Empty(loaded.IconMappings);
        Assert.Empty(loaded.InterruptKeys);
        Assert.False(loaded.HasDetectionRegion);
        Assert.Equal(legacyJson, await File.ReadAllTextAsync(path));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }
}
