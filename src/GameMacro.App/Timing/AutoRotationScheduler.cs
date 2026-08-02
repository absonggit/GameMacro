using GameMacro.Core.Models;
using GameMacro.App.Detection;

namespace GameMacro.App.Timing;

public sealed class AutoRotationScheduler(HybridCooldownTracker tracker)
{
    private DateTimeOffset _globalLockEndsAt = DateTimeOffset.MinValue;

    public MacroRule? TrySelect(IEnumerable<MacroRule> rules, DateTimeOffset now)
    {
        if (IsGloballyLocked(now)) return null;
        return rules
            .Where(rule => rule.Enabled)
            .OrderBy(rule => rule.Priority)
            .FirstOrDefault(rule => tracker.GetSnapshot(rule, now).State == HybridSkillState.Ready);
    }

    public MacroRule? TrySelectVisual(IEnumerable<MacroRule> rules, DateTimeOffset now, Func<MacroRule, IconVisualState> getVisualState)
    {
        return rules.Where(rule => rule.Enabled)
            .OrderBy(rule => rule.Priority)
            .FirstOrDefault(rule => getVisualState(rule) == IconVisualState.Ready);
    }

    public void RecordKeySent(DateTimeOffset now) { }

    public void RecordSuccessfulCast(MacroRule rule, DateTimeOffset now)
        => RecordConfirmedCast(rule, now, now);

    public void RecordConfirmedCast(MacroRule rule, DateTimeOffset confirmedAt, DateTimeOffset keySentAt)
    {
        tracker.OnPhysicalKey(rule, confirmedAt);
        _globalLockEndsAt = keySentAt.AddSeconds(Math.Max(0, rule.CastTimeSeconds) + 1);
    }

    public bool IsGloballyLocked(DateTimeOffset now) => now < _globalLockEndsAt;

    public double GlobalCooldownRemaining(DateTimeOffset now) => Math.Max(0, (_globalLockEndsAt - now).TotalSeconds);
}
