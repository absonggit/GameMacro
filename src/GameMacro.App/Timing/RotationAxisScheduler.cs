using GameMacro.App.Detection;
using GameMacro.Core.Models;

namespace GameMacro.App.Timing;

public sealed class RotationAxisScheduler
{
    private readonly HashSet<Guid> _blockedUntilUnavailable = [];
    private List<MacroRule>? _activeBurst;
    private int _burstIndex;
    private int _burstRepeatIndex;
    private DateTimeOffset _nextBurstActionAt = DateTimeOffset.MinValue;
    private int _fillerIndex;

    public MacroRule? Select(MacroProfile profile, IReadOnlyDictionary<Guid, IconVisualState> states)
        => Select(profile, states, DateTimeOffset.UtcNow);

    public MacroRule? Select(MacroProfile profile, IReadOnlyDictionary<Guid, IconVisualState> states, DateTimeOffset now)
    {
        UpdateBlockedStates(states);
        bool IsReady(MacroRule rule) => states.TryGetValue(rule.Id, out var state)
            && state == IconVisualState.Ready && !_blockedUntilUnavailable.Contains(rule.Id);
        var enabled = profile.Rules.Where(rule => rule.Enabled).ToDictionary(rule => rule.Id);
        List<MacroRule> Resolve(IEnumerable<Guid> ids) => ids
            .Where(enabled.ContainsKey).Select(id => enabled[id]).ToList();

        if (_activeBurst is not null)
        {
            if (_burstIndex >= _activeBurst.Count || now < _nextBurstActionAt) return null;
            var current = _activeBurst[_burstIndex];
            var scheduledRepeat = current.AllowNoCooldownBurstRepeat && _burstRepeatIndex > 0;
            return scheduledRepeat || IsReady(current) ? current : null;
        }

        var burst = Resolve(profile.BurstAxisRuleIds);
        if (burst.Count > 0 && burst.All(IsReady))
        {
            _activeBurst = burst;
            _burstIndex = 0;
            _burstRepeatIndex = 0;
            _nextBurstActionAt = now.AddMilliseconds(burst[0].BurstInitialDelayMs);
            return now >= _nextBurstActionAt ? burst[0] : null;
        }

        var basePriority = Resolve(profile.BasePriorityRuleIds);
        if (basePriority.Count > 0) return basePriority.FirstOrDefault(IsReady);

        if (profile.BurstAxisRuleIds.Count > 0 || profile.BasePriorityRuleIds.Count > 0) return null;
        return Select(profile.Rules, states);
    }

    public MacroRule? Select(IReadOnlyList<MacroRule> rules, IReadOnlyDictionary<Guid, IconVisualState> states)
    {
        UpdateBlockedStates(states);

        bool IsReady(MacroRule rule) => states.TryGetValue(rule.Id, out var state)
            && state == IconVisualState.Ready && !_blockedUntilUnavailable.Contains(rule.Id);

        if (_activeBurst is not null)
            return _burstIndex < _activeBurst.Count && IsReady(_activeBurst[_burstIndex])
                ? _activeBurst[_burstIndex]
                : null;

        var ordered = rules.Where(rule => rule.Enabled).OrderBy(rule => rule.Priority).ToList();
        var burst = ordered.Where(rule => rule.IsBurstAxisMember).ToList();
        if (burst.Count > 0 && burst.All(IsReady))
        {
            _activeBurst = burst;
            _burstIndex = 0;
            return burst[0];
        }

        var insert = ordered.FirstOrDefault(rule => rule.IsShortCooldownInsert && IsReady(rule));
        if (insert is not null) return insert;

        var filler = ordered.Where(rule => rule.IsFillerAxisMember).ToList();
        if (filler.Count > 0)
        {
            _fillerIndex %= filler.Count;
            return IsReady(filler[_fillerIndex]) ? filler[_fillerIndex] : null;
        }

        var hasAxisConfiguration = ordered.Any(rule => rule.IsBurstAxisMember
            || rule.IsFillerAxisMember || rule.IsShortCooldownInsert);
        return hasAxisConfiguration ? null : ordered.FirstOrDefault(IsReady);
    }

    private void UpdateBlockedStates(IReadOnlyDictionary<Guid, IconVisualState> states)
    {
        foreach (var id in _blockedUntilUnavailable.ToArray())
            if (!states.TryGetValue(id, out var state) || state != IconVisualState.Ready)
                _blockedUntilUnavailable.Remove(id);
    }

    public void RecordReleased(MacroRule rule)
        => RecordReleased(rule, DateTimeOffset.UtcNow);

    public void RecordReleased(MacroRule rule, DateTimeOffset now)
    {
        if (_activeBurst is not null)
        {
            if (_burstIndex < _activeBurst.Count && _activeBurst[_burstIndex].Id == rule.Id)
            {
                var repetitions = rule.AllowNoCooldownBurstRepeat ? Math.Max(1, rule.BurstRepeatCount) : 1;
                _burstRepeatIndex++;
                if (_burstRepeatIndex < repetitions)
                {
                    _nextBurstActionAt = now.AddMilliseconds(rule.BurstRepeatIntervalMs);
                    return;
                }
                _blockedUntilUnavailable.Add(rule.Id);
                _burstIndex++;
                _burstRepeatIndex = 0;
                if (_burstIndex >= _activeBurst.Count)
                {
                    _activeBurst = null;
                    _burstIndex = 0;
                    _nextBurstActionAt = DateTimeOffset.MinValue;
                }
                else _nextBurstActionAt = now.AddMilliseconds(_activeBurst[_burstIndex].BurstInitialDelayMs);
            }
            return;
        }

        _blockedUntilUnavailable.Add(rule.Id);
        if (rule.IsFillerAxisMember && !rule.IsShortCooldownInsert) _fillerIndex++;
    }

    public void Reset()
    {
        _blockedUntilUnavailable.Clear();
        _activeBurst = null;
        _burstIndex = 0;
        _burstRepeatIndex = 0;
        _nextBurstActionAt = DateTimeOffset.MinValue;
        _fillerIndex = 0;
    }

    public bool IsActiveNoCooldownRepeat(MacroRule rule) => _activeBurst is not null
        && _burstIndex < _activeBurst.Count && _activeBurst[_burstIndex].Id == rule.Id
        && rule.AllowNoCooldownBurstRepeat && rule.BurstRepeatCount > 1;
}
