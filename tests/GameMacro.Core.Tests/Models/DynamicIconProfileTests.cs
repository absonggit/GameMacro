using GameMacro.Core.Models;
using System.Text.Json;

namespace GameMacro.Core.Tests.Models;

public sealed class DynamicIconProfileTests
{
    [Fact]
    public void Dynamic_mapping_configuration_round_trips()
    {
        var profile = ValidProfile();
        profile.SourceX = .1;
        profile.SourceY = .7;
        profile.SourceWidth = .8;
        profile.SourceHeight = .2;
        profile.SourcePreviewPng = "source-png";

        var restored = JsonSerializer.Deserialize<MacroProfile>(JsonSerializer.Serialize(profile))!;

        Assert.Equal(2, restored.Version);
        Assert.True(restored.HasDetectionRegion);
        Assert.Equal(20, restored.ScanIntervalMs);
        Assert.Equal("F1", restored.IconMappings.Single().ActionKey);
        Assert.Equal([1d, 2d], restored.IconMappings.Single().Signature);
        Assert.True(restored.IconMappings.Single().IsCalibrated);
        Assert.True(restored.HasSourceRegion);
        Assert.Equal("source-png", restored.SourcePreviewPng);
    }

    [Fact]
    public void Toggle_hotkey_cannot_equal_enabled_mapping_key()
    {
        var profile = ValidProfile();
        profile.ToggleHotkey = profile.IconMappings[0].ActionKey;

        Assert.Contains(ProfileInputValidator.Validate(profile), error => error.Contains("冲突"));
    }

    [Theory]
    [InlineData(9)]
    [InlineData(501)]
    public void Scan_interval_must_be_between_ten_and_five_hundred_milliseconds(int interval)
    {
        var profile = ValidProfile();
        profile.ScanIntervalMs = interval;

        Assert.Contains(ProfileInputValidator.Validate(profile), error => error.Contains("扫描间隔"));
    }

    [Fact]
    public void Enabled_mapping_must_have_a_captured_template()
    {
        var profile = ValidProfile();
        profile.IconMappings[0].Signature = [];

        Assert.Contains(ProfileInputValidator.Validate(profile), error => error.Contains("捕获"));
    }

    [Fact]
    public void Runnable_profile_requires_shared_region_and_enabled_mapping()
    {
        var noRegion = ValidProfile();
        noRegion.DetectionWidth = 0;
        var noMappings = ValidProfile();
        noMappings.IconMappings.Clear();

        Assert.Contains(ProfileInputValidator.Validate(noRegion), error => error.Contains("框选"));
        Assert.Contains(ProfileInputValidator.Validate(noMappings), error => error.Contains("映射"));
    }

    [Fact]
    public void Profile_round_trip_preserves_interrupt_keys()
    {
        var profile = ValidProfile();
        profile.InterruptKeys = ["Q", "F3"];

        var restored = JsonSerializer.Deserialize<MacroProfile>(JsonSerializer.Serialize(profile))!;

        Assert.Equal(["Q", "F3"], restored.InterruptKeys);
    }

    [Fact]
    public void Legacy_profile_without_interrupt_keys_uses_an_empty_list()
    {
        var restored = JsonSerializer.Deserialize<MacroProfile>("{\"Name\":\"legacy\"}")!;

        Assert.Empty(restored.InterruptKeys);
    }

    [Fact]
    public void Interrupt_key_can_equal_a_skill_mapping_key()
    {
        var profile = ValidProfile();
        profile.InterruptKeys = [profile.IconMappings[0].ActionKey];

        Assert.DoesNotContain(ProfileInputValidator.Validate(profile), error => error.Contains("优先打断键"));
    }

    [Fact]
    public void Interrupt_key_cannot_equal_the_toggle_hotkey()
    {
        var profile = ValidProfile();
        profile.InterruptKeys = [profile.ToggleHotkey];

        Assert.Contains(ProfileInputValidator.Validate(profile), error => error.Contains("优先打断键") && error.Contains("冲突"));
    }

    [Fact]
    public void Interrupt_keys_must_be_supported_and_unique()
    {
        var unsupported = ValidProfile();
        unsupported.InterruptKeys = ["Space"];
        var duplicated = ValidProfile();
        duplicated.InterruptKeys = ["Q", "q"];

        Assert.Contains(ProfileInputValidator.Validate(unsupported), error => error.Contains("优先打断键") && error.Contains("不受支持"));
        Assert.Contains(ProfileInputValidator.Validate(duplicated), error => error.Contains("优先打断键") && error.Contains("重复"));
    }

    [Fact]
    public void Referenced_mapping_is_valid_when_template_exists_and_missing_template_is_reported()
    {
        var profile = ValidProfile();
        var templateId = Guid.NewGuid();
        profile.IconMappings[0].SkillTemplateId = templateId;
        profile.IconMappings[0].Signature = [];
        profile.IconMappings[0].PreviewPng = string.Empty;
        profile.IconMappings[0].MatchThreshold = 0;

        Assert.DoesNotContain(ProfileInputValidator.Validate(profile, []), error => error.Contains("捕获"));
        Assert.Contains(ProfileInputValidator.Validate(profile, [templateId]), error => error.Contains("模板缺失"));
    }

    [Fact]
    public void Same_skill_template_cannot_be_referenced_twice_in_one_profile()
    {
        var profile = ValidProfile();
        var templateId = Guid.NewGuid();
        profile.IconMappings =
        [
            new IconKeyMapping { SkillTemplateId = templateId, ActionKey = "1" },
            new IconKeyMapping { SkillTemplateId = templateId, ActionKey = "2" }
        ];

        Assert.Contains(ProfileInputValidator.Validate(profile), error => error.Contains("重复添加"));
    }

    private static MacroProfile ValidProfile() => new()
    {
        Version = 2,
        ToggleHotkey = "F8",
        DetectionX = .6,
        DetectionY = .5,
        DetectionWidth = .05,
        DetectionHeight = .05,
        ScanIntervalMs = 20,
        IconMappings =
        [
            new IconKeyMapping
            {
                ActionKey = "F1",
                PreviewPng = "png",
                Signature = [1, 2],
                MatchThreshold = .2
            }
        ]
    };
}
