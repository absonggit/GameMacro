using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using GameMacro.Core.Models;
using GameMacro.Core.Storage;

namespace GameMacro.App.Services;

public sealed class JsonProfileStore(string directory) : IProfileStore
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public string GetProfilePath(Guid profileId) => Path.Combine(directory, $"{profileId:N}.json");
    public string GetBackupPath(Guid profileId) => GetProfilePath(profileId) + ".bak";

    public async Task<IReadOnlyList<MacroProfile>> LoadAllAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(directory);
        List<MacroProfile> profiles = [];
        foreach (var path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
        {
            var profile = await ReadProfileAsync(path, cancellationToken);
            if (profile is not null)
                profiles.Add(profile);
        }
        return profiles.OrderBy(profile => profile.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public async Task SaveAsync(MacroProfile profile, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(directory);
        var path = GetProfilePath(profile.Id);
        var tempPath = path + ".tmp";
        var backupPath = GetBackupPath(profile.Id);
        var json = JsonSerializer.Serialize(profile, _options);
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);
        if (File.Exists(path))
            File.Copy(path, backupPath, true);
        File.Move(tempPath, path, true);
    }

    public void Save(MacroProfile profile)
    {
        Directory.CreateDirectory(directory);
        var path = GetProfilePath(profile.Id);
        var tempPath = path + ".tmp";
        var backupPath = GetBackupPath(profile.Id);
        File.WriteAllText(tempPath, JsonSerializer.Serialize(profile, _options));
        if (File.Exists(path)) File.Copy(path, backupPath, true);
        File.Move(tempPath, path, true);
    }

    public Task DeleteAsync(Guid profileId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetProfilePath(profileId);
        if (File.Exists(path)) File.Delete(path);
        var backupPath = GetBackupPath(profileId);
        if (File.Exists(backupPath)) File.Delete(backupPath);
        return Task.CompletedTask;
    }

    private async Task<MacroProfile?> ReadProfileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken);
            return UpgradeForEditing(JsonSerializer.Deserialize<MacroProfile>(json, _options));
        }
        catch (JsonException)
        {
            var corruptPath = path + ".corrupt";
            File.Copy(path, corruptPath, true);
            var backupPath = path + ".bak";
            if (!File.Exists(backupPath)) return null;
            var backupJson = await File.ReadAllTextAsync(backupPath, cancellationToken);
            return UpgradeForEditing(JsonSerializer.Deserialize<MacroProfile>(backupJson, _options));
        }
    }

    private static MacroProfile? UpgradeForEditing(MacroProfile? profile)
    {
        if (profile is null || profile.Version >= 2) return profile;
        return new MacroProfile
        {
            Version = 2,
            Id = profile.Id,
            Name = profile.Name,
            TargetWindowTitle = profile.TargetWindowTitle,
            ToggleHotkey = profile.ToggleHotkey,
            ScanIntervalMs = 20
        };
    }
}
