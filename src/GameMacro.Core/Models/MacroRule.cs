namespace GameMacro.Core.Models;

public sealed class MacroRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "新技能";
    public bool Enabled { get; set; } = true;
    public string ActionKey { get; set; } = "F1";
    public RuleMode Mode { get; set; } = RuleMode.Conditional;
    public int Priority { get; set; }
    public bool IsBurstAxisMember { get; set; }
    public bool IsFillerAxisMember { get; set; }
    public bool IsShortCooldownInsert { get; set; }
    public int BurstRepeatCount { get; set; } = 1;
    public int BurstInitialDelayMs { get; set; }
    public int BurstRepeatIntervalMs { get; set; } = 1000;
    public bool AllowNoCooldownBurstRepeat { get; set; }
    public int IntervalMs { get; set; } = 75;
    public int ProtectionMs { get; set; } = 300;
    public int RequiredStableSamples { get; set; } = 2;
    public double BaseCooldownSeconds { get; set; }
    public double CastTimeSeconds { get; set; }
    public double DetectionX { get; set; }
    public double DetectionY { get; set; }
    public double DetectionWidth { get; set; }
    public double DetectionHeight { get; set; }
    public double[] ReadySignature { get; set; } = [];
    public double[] CooldownSignature { get; set; } = [];
    public string ReadyPreviewPng { get; set; } = string.Empty;
    public string CooldownPreviewPng { get; set; } = string.Empty;
    public double ReadyThreshold { get; set; }
    public double ChangeThreshold { get; set; }
    public List<ReadyIconTemplate> AdditionalReadyIcons { get; set; } = [];
    public int NetworkMarginMs { get; set; } = 800;
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasVisualCalibration => DetectionWidth > 0 && DetectionHeight > 0
        && ReadySignature.Length > 0 && ReadyThreshold > 0 && ChangeThreshold > ReadyThreshold
        && !string.IsNullOrWhiteSpace(ReadyPreviewPng);
    [System.Text.Json.Serialization.JsonIgnore]
    public string CalibrationText => HasVisualCalibration ? $"已捕获 {1 + AdditionalReadyIcons.Count} 张" : "未捕获";
    [System.Text.Json.Serialization.JsonIgnore]
    public string SecondaryReadyPreviewPng => AdditionalReadyIcons.FirstOrDefault()?.PreviewPng ?? string.Empty;

    public IReadOnlyList<string> Validate()
    {
        List<string> errors = [];
        if (string.IsNullOrWhiteSpace(ActionKey))
            errors.Add("按键不能为空。");
        if (IntervalMs is < 20 or > 60_000)
            errors.Add("间隔必须在 20 到 60000 毫秒之间。");
        if (ProtectionMs is < 0 or > 60_000)
            errors.Add("触发保护必须在 0 到 60000 毫秒之间。");
        if (RequiredStableSamples < 1)
            errors.Add("稳定采样次数不能小于 1。");
        if (BurstRepeatCount is < 1 or > 20)
            errors.Add("爆发重复次数必须在 1 到 20 之间。");
        if (BurstInitialDelayMs is < 0 or > 30_000)
            errors.Add("爆发首次等待必须在 0 到 30000 毫秒之间。");
        if (BurstRepeatIntervalMs is < 20 or > 30_000)
            errors.Add("爆发重复间隔必须在 20 到 30000 毫秒之间。");
        if (BaseCooldownSeconds is < 0 or > 600)
            errors.Add("基础 CD 必须在 0 到 600 秒之间。");
        if (CastTimeSeconds is < 0 or > 30)
            errors.Add("施法时间必须在 0 到 30 秒之间。");
        if (NetworkMarginMs is < 100 or > 5000)
            errors.Add("网络余量必须在 100 到 5000 毫秒之间。");
        if (DetectionX < 0 || DetectionY < 0 || DetectionWidth < 0 || DetectionHeight < 0
            || DetectionX + DetectionWidth > 1 || DetectionY + DetectionHeight > 1)
            errors.Add("技能检测区域必须位于游戏客户区内。");
        return errors;
    }
}

public sealed class ReadyIconTemplate
{
    public double[] Signature { get; set; } = [];
    public string PreviewPng { get; set; } = string.Empty;
    public double ReadyThreshold { get; set; }
    public double ChangeThreshold { get; set; }
}

public enum RuleMode
{
    FixedInterval,
    Conditional
}
