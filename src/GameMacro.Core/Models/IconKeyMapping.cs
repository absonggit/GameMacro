using System.Text.Json.Serialization;

namespace GameMacro.Core.Models;

public sealed class IconKeyMapping
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SkillTemplateId { get; set; }
    public bool Enabled { get; set; } = true;
    public string ActionKey { get; set; } = "F1";
    public double[] Signature { get; set; } = [];
    public string PreviewPng { get; set; } = string.Empty;
    public double MatchThreshold { get; set; }
    public byte[] PixelTemplateData { get; set; } = [];

    [JsonIgnore]
    public bool IsCalibrated => Signature.Length > 0
        && MatchThreshold > 0
        && !string.IsNullOrWhiteSpace(PreviewPng);

}
