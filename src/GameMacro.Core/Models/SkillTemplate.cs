namespace GameMacro.Core.Models;

public sealed class SkillTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public double[] Signature { get; set; } = [];
    public string PreviewPng { get; set; } = string.Empty;
    public double MatchThreshold { get; set; } = .18;
    public byte[] PixelTemplateData { get; set; } = [];
}
