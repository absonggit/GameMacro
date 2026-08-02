using GameMacro.App.ViewModels;
using GameMacro.Core.Models;

namespace GameMacro.App.Detection;

public static class BatchMappingBuilder
{
    public static List<PendingIconMapping> Build(IEnumerable<DetectedSkillIcon> icons)
        => icons.Select(icon => new PendingIconMapping
        {
            PreviewPng = icon.PreviewPng,
            Signature = icon.Signature.ToArray(),
            MatchThreshold = icon.MatchThreshold,
            PixelTemplateData = PixelIconTemplateBuilder.Create(icon.Pixels, icon.Width, icon.Height).Serialize()
        }).ToList();

    public static List<PendingIconMapping> FromMappings(IEnumerable<IconKeyMapping> mappings)
        => mappings.Select(mapping => new PendingIconMapping
        {
            Id = mapping.Id,
            SkillTemplateId = mapping.SkillTemplateId,
            PreviewPng = mapping.PreviewPng,
            Signature = mapping.Signature.ToArray(),
            MatchThreshold = mapping.MatchThreshold,
            ActionKey = mapping.ActionKey,
            Enabled = mapping.Enabled,
            PixelTemplateData = mapping.PixelTemplateData.ToArray()
        }).ToList();

    public static List<PendingIconMapping> FromMappings(
        IEnumerable<IconKeyMapping> mappings,
        SkillLibrary library)
    {
        var templates = library.Templates.ToDictionary(template => template.Id);
        return mappings.Select(mapping =>
        {
            templates.TryGetValue(mapping.SkillTemplateId, out var template);
            return new PendingIconMapping
            {
                Id = mapping.Id,
                SkillTemplateId = mapping.SkillTemplateId,
                PreviewPng = template?.PreviewPng ?? mapping.PreviewPng,
                Signature = template?.Signature.ToArray() ?? mapping.Signature.ToArray(),
                MatchThreshold = template?.MatchThreshold ?? mapping.MatchThreshold,
                ActionKey = mapping.ActionKey,
                Enabled = mapping.Enabled,
                PixelTemplateData = template?.PixelTemplateData.ToArray() ?? mapping.PixelTemplateData.ToArray(),
                IsMissingTemplate = mapping.SkillTemplateId != Guid.Empty && template is null
            };
        }).ToList();
    }

    public static List<IconKeyMapping> Save(IEnumerable<PendingIconMapping> pendingItems)
    {
        var items = pendingItems.ToList();
        if (items.Count == 0) throw new InvalidOperationException("没有可保存的技能图标。");
        var unassigned = items.FirstOrDefault(item => !InputKeyOptions.All.Contains(item.ActionKey));
        if (unassigned is not null) throw new InvalidOperationException("请为每个技能图标选择按键后再保存。");
        var missing = items.FirstOrDefault(item => item.IsMissingTemplate);
        if (missing is not null) throw new InvalidOperationException("存在模板缺失的技能，请从方案中移除后再保存。");

        for (var left = 0; left < items.Count; left++)
        for (var right = left + 1; right < items.Count; right++)
        {
            if (items[left].Signature.Length == items[right].Signature.Length
                && IconVisualSignature.Distance(items[left].Signature, items[right].Signature) <= .06)
                throw new InvalidOperationException($"第 {left + 1} 和第 {right + 1} 个技能图标过于相似，请删除重复项或重新扫描。");
        }

        return items.Select(item => new IconKeyMapping
        {
            Id = item.Id,
            SkillTemplateId = item.SkillTemplateId,
            Enabled = item.Enabled,
            ActionKey = item.ActionKey,
            Signature = item.SkillTemplateId == Guid.Empty ? item.Signature.ToArray() : [],
            PreviewPng = item.SkillTemplateId == Guid.Empty ? item.PreviewPng : string.Empty,
            MatchThreshold = item.SkillTemplateId == Guid.Empty ? item.MatchThreshold : 0,
            PixelTemplateData = item.SkillTemplateId == Guid.Empty ? item.PixelTemplateData.ToArray() : []
        }).ToList();
    }
}
