using GameMacro.Core.Models;

namespace GameMacro.App.Timing;

public enum HybridSkillState
{
    Ready,
    Cooldown,
    Casting
}

public readonly record struct HybridCooldownSnapshot(HybridSkillState State, int RemainingSeconds);

public sealed class HybridCooldownTracker
{
    private readonly Dictionary<Guid, TimerState> _states = [];

    public void OnPhysicalKey(MacroRule rule, DateTimeOffset now)
    {
        _states[rule.Id] = new(
            now.AddSeconds(Math.Clamp(rule.BaseCooldownSeconds, 0, 600)),
            now.AddSeconds(Math.Clamp(rule.CastTimeSeconds, 0, 30)));
    }

    public HybridCooldownSnapshot GetSnapshot(MacroRule rule, DateTimeOffset now)
    {
        if (!_states.TryGetValue(rule.Id, out var state)) return new(HybridSkillState.Ready, 0);
        var remaining = Math.Max(0, (int)Math.Ceiling((state.CooldownEndsAt - now).TotalSeconds));
        if (state.CastEndsAt > now) return new(HybridSkillState.Casting, remaining);
        return remaining > 0
            ? new(HybridSkillState.Cooldown, remaining)
            : new(HybridSkillState.Ready, 0);
    }

    private readonly record struct TimerState(DateTimeOffset CooldownEndsAt, DateTimeOffset CastEndsAt);
}
