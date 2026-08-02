using GameMacro.App.Detection;
using GameMacro.Core.Models;

namespace GameMacro.App.Timing;

public enum ReleaseDecision { None, SendKey, Confirmed, TimedOut }

public sealed class ClosedLoopReleaseController
{
    private readonly StableStateDetector _readyDetector = new(2);
    private readonly StableStateDetector _cooldownDetector = new(2);
    private Guid? _candidateRuleId;
    private MacroRule? _pendingRule;
    private DateTimeOffset _confirmationDeadline;

    public MacroRule? PendingRule => _pendingRule;
    public DateTimeOffset? PendingSince { get; private set; }

    public ReleaseDecision Observe(MacroRule rule, IconVisualState visualState, DateTimeOffset now)
    {
        if (_pendingRule is not null)
        {
            if (now > _confirmationDeadline)
            {
                Reset();
                return ReleaseDecision.TimedOut;
            }
            if (rule.Id != _pendingRule.Id) return ReleaseDecision.None;
            if (!_cooldownDetector.Observe(visualState) || _cooldownDetector.ConfirmedState != IconVisualState.Cooldown)
                return ReleaseDecision.None;
            Reset();
            return ReleaseDecision.Confirmed;
        }

        if (_candidateRuleId != rule.Id)
        {
            _candidateRuleId = rule.Id;
            _readyDetector.Reset();
        }
        if (!_readyDetector.Observe(visualState) || _readyDetector.ConfirmedState != IconVisualState.Ready)
            return ReleaseDecision.None;
        _pendingRule = rule;
        PendingSince = now;
        _confirmationDeadline = now.AddSeconds(Math.Max(0, rule.CastTimeSeconds)).AddMilliseconds(rule.NetworkMarginMs);
        _cooldownDetector.Reset();
        _readyDetector.Reset();
        return ReleaseDecision.SendKey;
    }

    public void CancelPending() => Reset();

    private void Reset()
    {
        _pendingRule = null;
        PendingSince = null;
        _candidateRuleId = null;
        _readyDetector.Reset();
        _cooldownDetector.Reset();
    }
}
