using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameMacro.Core.Models;

namespace GameMacro.App.Services;

public static class ProfileTransfer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static string Serialize(MacroProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return JsonSerializer.Serialize(profile, Options);
    }

    public static string SerializeLegacy(MacroProfile profile) => Serialize(profile);

    public static string Serialize(MacroProfile profile, SkillLibrary library)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(library);
        var templateIds = profile.IconMappings
            .Select(mapping => mapping.SkillTemplateId)
            .Where(id => id != Guid.Empty)
            .ToHashSet();
        var templates = library.Templates
            .Where(template => templateIds.Contains(template.Id))
            .ToList();
        var categoryIds = templates.Select(template => template.CategoryId).ToHashSet();
        return JsonSerializer.Serialize(new ProfileExportPackage
        {
            Profile = profile,
            Templates = templates,
            Categories = library.Categories
                .Where(category => categoryIds.Contains(category.Id))
                .ToList()
        }, Options);
    }

    public static ProfileExportPackage DeserializePackage(string json)
        => JsonSerializer.Deserialize<ProfileExportPackage>(json, Options)
           ?? throw new InvalidDataException("方案包不包含有效配置。");

    public static MacroProfile ImportAsCopy(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidDataException("方案文件内容为空。");

        try
        {
            var profile = JsonSerializer.Deserialize<MacroProfile>(json, Options)
                ?? throw new InvalidDataException("方案文件不包含有效配置。");
            profile.Version = 2;
            profile.Id = Guid.NewGuid();
            profile.Name = $"{(string.IsNullOrWhiteSpace(profile.Name) ? "导入方案" : profile.Name.Trim())}（导入）";
            return profile;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("方案文件不是有效的 JSON 配置。", exception);
        }
    }

    public static ProfileImportResult ImportAsCopy(string json, SkillLibrary library)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidDataException("方案文件内容为空。");
        ArgumentNullException.ThrowIfNull(library);
        try
        {
            using var document = JsonDocument.Parse(json);
            MacroProfile profile;
            var changed = false;
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("profile", out _))
            {
                var package = DeserializePackage(json);
                var merged = SkillLibraryPackageMerger.Merge(package, library);
                profile = merged.Profile;
                changed = merged.LibraryChanged;
            }
            else
            {
                profile = JsonSerializer.Deserialize<MacroProfile>(json, Options)
                    ?? throw new InvalidDataException("方案文件不包含有效配置。");
            }
            var migrated = LegacySkillLibraryMigrator.Migrate(profile, library);
            changed |= migrated.AddedTemplates > 0;
            profile.Id = Guid.NewGuid();
            profile.Version = 3;
            profile.Name = $"{(string.IsNullOrWhiteSpace(profile.Name) ? "导入方案" : profile.Name.Trim())}（导入）";
            return new ProfileImportResult(profile, changed);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("方案文件不是有效的 JSON 配置。", exception);
        }
    }
}
