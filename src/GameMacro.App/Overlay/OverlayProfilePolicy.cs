using GameMacro.Core.Models;

namespace GameMacro.App.Overlay;

public static class OverlayProfilePolicy
{
    public static IReadOnlyList<MacroProfile> ProfilesForTarget(
        IEnumerable<MacroProfile> profiles,
        MacroProfile current)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(current);

        if (!string.IsNullOrWhiteSpace(current.TargetProcessName))
        {
            return profiles
                .Where(profile => string.Equals(
                    profile.TargetProcessName?.Trim(),
                    current.TargetProcessName.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var title = current.TargetWindowTitle?.Trim() ?? string.Empty;
        return profiles
            .Where(profile => string.Equals(
                profile.TargetWindowTitle?.Trim(),
                title,
                StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static bool CanSwitch(bool isRunning) => !isRunning;
}
