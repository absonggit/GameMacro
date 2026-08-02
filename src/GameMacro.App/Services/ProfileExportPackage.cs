using GameMacro.Core.Models;

namespace GameMacro.App.Services;

public sealed class ProfileExportPackage
{
    public int FormatVersion { get; set; } = 2;
    public MacroProfile Profile { get; set; } = new();
    public List<SkillCategory> Categories { get; set; } = [];
    public List<SkillTemplate> Templates { get; set; } = [];
}

public sealed record ProfileImportResult(MacroProfile Profile, bool LibraryChanged);

public sealed record SkillLibraryMergeResult(MacroProfile Profile, bool LibraryChanged);
