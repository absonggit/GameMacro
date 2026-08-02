namespace GameMacro.Core.Models;

public sealed class SkillCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "未分类";
}
