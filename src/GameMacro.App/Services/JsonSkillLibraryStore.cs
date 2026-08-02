using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameMacro.Core.Models;

namespace GameMacro.App.Services;

public sealed class JsonSkillLibraryStore(string path)
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public string Path => path;
    public string BackupPath => path + ".bak";
    public string CorruptPath => path + ".corrupt";

    public async Task<SkillLibrary> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return NewLibrary();
        try
        {
            return Normalize(await ReadAsync(path, cancellationToken));
        }
        catch (JsonException mainException)
        {
            File.Copy(path, CorruptPath, true);
            if (!File.Exists(BackupPath))
                throw new InvalidDataException("技能库文件已损坏，且没有可用备份。", mainException);
            try
            {
                return Normalize(await ReadAsync(BackupPath, cancellationToken));
            }
            catch (JsonException backupException)
            {
                throw new InvalidDataException("技能库文件及其备份均已损坏。", backupException);
            }
        }
    }

    public async Task SaveAsync(SkillLibrary library, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(library);
        var directory = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        var temporaryPath = path + ".tmp";
        await File.WriteAllTextAsync(
            temporaryPath,
            JsonSerializer.Serialize(Normalize(library), _options),
            cancellationToken);
        if (File.Exists(path)) File.Copy(path, BackupPath, true);
        File.Move(temporaryPath, path, true);
    }

    private async Task<SkillLibrary> ReadAsync(string filePath, CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return JsonSerializer.Deserialize<SkillLibrary>(json, _options)
            ?? throw new JsonException("技能库文件不包含有效数据。");
    }

    private static SkillLibrary NewLibrary()
    {
        var library = new SkillLibrary();
        new SkillLibraryCatalog(library).EnsureUncategorized();
        return library;
    }

    private static SkillLibrary Normalize(SkillLibrary library)
    {
        library.Categories ??= [];
        library.Templates ??= [];
        new SkillLibraryCatalog(library).EnsureUncategorized();
        return library;
    }
}
