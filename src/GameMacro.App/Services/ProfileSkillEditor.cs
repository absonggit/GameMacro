using GameMacro.Core.Models;

namespace GameMacro.App.Services;

public sealed record AddProfileSkillResult(IconKeyMapping Mapping, bool Added);

public sealed class ProfileSkillEditor
{
    public AddProfileSkillResult AddTemplate(MacroProfile profile, Guid templateId)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (templateId == Guid.Empty) throw new InvalidOperationException("技能模板 ID 无效。");
        var existing = profile.IconMappings.FirstOrDefault(mapping =>
            mapping.SkillTemplateId == templateId);
        if (existing is not null) return new AddProfileSkillResult(existing, false);
        var mapping = new IconKeyMapping
        {
            SkillTemplateId = templateId,
            ActionKey = "点击设置",
            Enabled = true
        };
        profile.IconMappings.Add(mapping);
        return new AddProfileSkillResult(mapping, true);
    }

    public bool RemoveMapping(MacroProfile profile, Guid mappingId)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var mapping = profile.IconMappings.FirstOrDefault(item => item.Id == mappingId);
        return mapping is not null && profile.IconMappings.Remove(mapping);
    }

    public void AssignKey(MacroProfile profile, Guid mappingId, string key)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var canonical = InputKeyOptions.All.FirstOrDefault(option =>
            string.Equals(option, key, StringComparison.OrdinalIgnoreCase));
        if (canonical is null) throw new InvalidOperationException($"按键 {key} 不受支持。");
        var mapping = profile.IconMappings.FirstOrDefault(item => item.Id == mappingId)
            ?? throw new KeyNotFoundException("方案技能映射不存在。");
        mapping.ActionKey = canonical;
    }
}
