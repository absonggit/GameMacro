namespace GameMacro.Core.Models;

public sealed class MacroProfile
{
    public int Version { get; set; } = 3;
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "新方案";
    public string TargetWindowTitle { get; set; } = string.Empty;
    public string TargetProcessName { get; set; } = string.Empty;
    public string ToggleHotkey { get; set; } = "F8";
    public List<string> InterruptKeys { get; set; } = [];
    public int ScanIntervalMs { get; set; } = 20;
    public double DetectionX { get; set; }
    public double DetectionY { get; set; }
    public double DetectionWidth { get; set; }
    public double DetectionHeight { get; set; }
    public string DetectionPreviewPng { get; set; } = string.Empty;
    public double SourceX { get; set; }
    public double SourceY { get; set; }
    public double SourceWidth { get; set; }
    public double SourceHeight { get; set; }
    public string SourcePreviewPng { get; set; } = string.Empty;
    public List<IconKeyMapping> IconMappings { get; set; } = [];
    public bool ShowGameOverlay { get; set; } = true;
    public double? OverlayLeft { get; set; }
    public double? OverlayTop { get; set; }
    public List<MacroRule> Rules { get; set; } = [];
    public List<Guid> BurstAxisRuleIds { get; set; } = [];
    public List<Guid> BasePriorityRuleIds { get; set; } = [];

    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasDetectionRegion => DetectionWidth > 0 && DetectionHeight > 0
        && DetectionX >= 0 && DetectionY >= 0
        && DetectionX + DetectionWidth <= 1
        && DetectionY + DetectionHeight <= 1;

    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasSourceRegion => SourceWidth > 0 && SourceHeight > 0
        && SourceX >= 0 && SourceY >= 0
        && SourceX + SourceWidth <= 1
        && SourceY + SourceHeight <= 1;

    public IEnumerable<MacroRule> OrderedConditionalRules() => Rules
        .Where(rule => rule.Enabled && rule.Mode == RuleMode.Conditional)
        .OrderBy(rule => rule.Priority);
}
