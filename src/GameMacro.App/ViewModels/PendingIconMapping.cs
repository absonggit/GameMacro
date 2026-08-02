namespace GameMacro.App.ViewModels;

public sealed class PendingIconMapping
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SkillTemplateId { get; set; }
    public string PreviewPng { get; set; } = string.Empty;
    public double[] Signature { get; set; } = [];
    public double MatchThreshold { get; set; } = .18;
    public string ActionKey { get; set; } = "点击设置";
    public bool Enabled { get; set; } = true;
    public byte[] PixelTemplateData { get; set; } = [];
    public bool IsMissingTemplate { get; set; }
}
