namespace GameMacro.App.ViewModels;

public sealed class SkillTemplateCard
{
    public Guid TemplateId { get; init; }
    public string PreviewPng { get; init; } = string.Empty;
    public bool IsAdded { get; init; }
}
