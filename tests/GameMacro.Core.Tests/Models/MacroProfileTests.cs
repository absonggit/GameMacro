using GameMacro.Core.Models;
using System.Text.Json;

namespace GameMacro.Core.Tests.Models;

public sealed class MacroProfileTests
{
    [Fact]
    public void Validate_rejects_non_positive_interval()
    {
        var rule = new MacroRule { IntervalMs = 0 };

        Assert.Contains(rule.Validate(), error => error.Contains("间隔", StringComparison.Ordinal));
    }

    [Fact]
    public void OrderedConditionalRules_returns_enabled_rules_by_priority()
    {
        var profile = new MacroProfile
        {
            Rules =
            [
                new() { Name = "F2", Enabled = true, Mode = RuleMode.Conditional, Priority = 2 },
                new() { Name = "Disabled", Enabled = false, Mode = RuleMode.Conditional, Priority = 0 },
                new() { Name = "Loop", Enabled = true, Mode = RuleMode.FixedInterval, Priority = 0 },
                new() { Name = "F1", Enabled = true, Mode = RuleMode.Conditional, Priority = 1 }
            ]
        };

        Assert.Equal(["F1", "F2"], profile.OrderedConditionalRules().Select(rule => rule.Name));
    }

    [Fact]
    public void Visual_calibration_survives_json_round_trip()
    {
        var rule = new MacroRule
        {
            DetectionX = .1, DetectionY = .2, DetectionWidth = .03, DetectionHeight = .04,
            ReadySignature = [0.1, -0.1], ReadyThreshold = .1, ChangeThreshold = .4,
            ReadyPreviewPng = "png"
        };

        var restored = JsonSerializer.Deserialize<MacroRule>(JsonSerializer.Serialize(rule))!;

        Assert.Equal(.1, restored.DetectionX);
        Assert.Equal(.04, restored.DetectionHeight);
        Assert.Equal([0.1, -0.1], restored.ReadySignature);
        Assert.Equal(.4, restored.ChangeThreshold);
        Assert.True(restored.HasVisualCalibration);
    }

    [Fact]
    public void Calibration_previews_survive_json_round_trip()
    {
        var rule = new MacroRule { ReadyPreviewPng = "ready-png", CooldownPreviewPng = "cooldown-png" };

        var restored = JsonSerializer.Deserialize<MacroRule>(JsonSerializer.Serialize(rule))!;

        Assert.Equal("ready-png", restored.ReadyPreviewPng);
        Assert.Equal("cooldown-png", restored.CooldownPreviewPng);
    }

    [Fact]
    public void Preset_keys_include_functions_digits_and_all_letters()
    {
        Assert.Equal(49, InputKeyOptions.All.Count);
        Assert.Contains("F12", InputKeyOptions.All);
        Assert.Contains("0", InputKeyOptions.All);
        Assert.Contains("Z", InputKeyOptions.All);
        Assert.Contains("~", InputKeyOptions.All);
    }

    [Fact]
    public void Profile_validation_rejects_control_and_skill_hotkey_conflicts()
    {
        var profile = new MacroProfile
        {
            ToggleHotkey = "F8",
            Rules = [new() { Enabled = true, ActionKey = "F8" }]
        };

        Assert.NotEmpty(ProfileInputValidator.Validate(profile));
    }

    [Fact]
    public void Legacy_emergency_hotkey_field_is_ignored_when_profile_is_loaded()
    {
        const string json = """{"Name":"旧方案","ToggleHotkey":"F8","EmergencyHotkey":"F12","Rules":[]}""";

        var profile = JsonSerializer.Deserialize<MacroProfile>(json);

        Assert.NotNull(profile);
        Assert.Equal("旧方案", profile.Name);
        Assert.Equal("F8", profile.ToggleHotkey);
    }

    [Fact]
    public void Reorder_inserts_rule_and_renumbers_priorities()
    {
        var rules = new List<MacroRule> { new() { ActionKey = "F1" }, new() { ActionKey = "F2" }, new() { ActionKey = "F3" } };

        RuleOrder.Move(rules, rules[0], rules[2]);

        Assert.Equal(["F2", "F3", "F1"], rules.Select(rule => rule.ActionKey));
        Assert.Equal([1, 2, 3], rules.Select(rule => rule.Priority));
    }

    [Fact]
    public void Overlay_position_survives_json_round_trip()
    {
        var profile = new MacroProfile { OverlayLeft = 123.5, OverlayTop = 45.5 };
        var restored = JsonSerializer.Deserialize<MacroProfile>(JsonSerializer.Serialize(profile))!;
        Assert.Equal(123.5, restored.OverlayLeft);
        Assert.Equal(45.5, restored.OverlayTop);
    }
}
