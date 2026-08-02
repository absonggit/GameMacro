namespace GameMacro.Core.Models;

public static class ProfileInputValidator
{
    public static IReadOnlyList<string> Validate(MacroProfile profile)
        => Validate(profile, []);

    public static IReadOnlyList<string> Validate(
        MacroProfile profile,
        IReadOnlyCollection<Guid> missingTemplateIds)
    {
        List<string> errors = [];
        var interruptKeys = profile.InterruptKeys ?? [];
        foreach (var key in interruptKeys)
        {
            if (!InputKeyOptions.All.Contains(key, StringComparer.OrdinalIgnoreCase))
                errors.Add($"优先打断键 {key} 不受支持。");
        }
        if (interruptKeys.GroupBy(key => key, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            errors.Add("优先打断键存在重复项。");
        if (interruptKeys.Contains(profile.ToggleHotkey, StringComparer.OrdinalIgnoreCase))
            errors.Add($"优先打断键 {profile.ToggleHotkey} 与启停热键冲突。");
        if (!InputKeyOptions.All.Contains(profile.ToggleHotkey)) errors.Add("启动热键不在预置列表中。");
        if (profile.ScanIntervalMs is < 10 or > 500)
            errors.Add("扫描间隔必须在 10 到 500 毫秒之间。");
        if (!profile.HasDetectionRegion && profile.IconMappings.Count > 0)
            errors.Add("请先框选动态技能图标区域。");
        if (!profile.IconMappings.Any(mapping => mapping.Enabled))
            errors.Add("请至少添加一个启用的图标按键映射。");
        foreach (var mapping in profile.IconMappings.Where(mapping => mapping.Enabled))
        {
            if (!InputKeyOptions.All.Contains(mapping.ActionKey))
                errors.Add($"映射按键 {mapping.ActionKey} 不在预置列表中。");
            if (mapping.SkillTemplateId == Guid.Empty && !mapping.IsCalibrated)
                errors.Add($"映射按键 {mapping.ActionKey} 尚未捕获图标。");
        }
        if (profile.IconMappings
            .Where(mapping => mapping.SkillTemplateId != Guid.Empty)
            .GroupBy(mapping => mapping.SkillTemplateId)
            .Any(group => group.Count() > 1))
            errors.Add("同一个技能模板不能在当前方案中重复添加。");
        if (missingTemplateIds.Count > 0)
            errors.Add($"当前方案有 {missingTemplateIds.Count} 个技能模板缺失，请重新从技能库添加。");
        var skillKeys = profile.IconMappings.Where(mapping => mapping.Enabled).Select(mapping => mapping.ActionKey)
            .Concat(profile.Rules.Where(rule => rule.Enabled).Select(rule => rule.ActionKey)).ToHashSet();
        if (skillKeys.Contains(profile.ToggleHotkey))
            errors.Add("启动热键与启用技能按键冲突。");
        return errors;
    }
}
