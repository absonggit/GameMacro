namespace GameMacro.Core.Models;

public sealed class SkillLibrary
{
    public int Version { get; set; } = 1;
    public List<SkillCategory> Categories { get; set; } = [];
    public List<SkillTemplate> Templates { get; set; } = [];
}
