using GameMacro.App.Detection;
using GameMacro.App.Timing;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Timing;

public sealed class ClosedLoopReleaseControllerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Two_ready_frames_request_key_but_do_not_start_cooldown()
    {
        var rule = new MacroRule { BaseCooldownSeconds = 10 };
        var tracker = new HybridCooldownTracker();
        var controller = new ClosedLoopReleaseController();

        Assert.Equal(ReleaseDecision.None, controller.Observe(rule, IconVisualState.Ready, Now));
        Assert.Equal(ReleaseDecision.SendKey, controller.Observe(rule, IconVisualState.Ready, Now));
        Assert.Equal(HybridSkillState.Ready, tracker.GetSnapshot(rule, Now).State);
    }

    [Fact]
    public void Two_cooldown_frames_confirm_cast()
    {
        var rule = new MacroRule();
        var controller = AwaitingCooldown(rule);

        Assert.Equal(ReleaseDecision.None, controller.Observe(rule, IconVisualState.Cooldown, Now.AddMilliseconds(100)));
        Assert.Equal(ReleaseDecision.Confirmed, controller.Observe(rule, IconVisualState.Cooldown, Now.AddMilliseconds(150)));
    }

    [Fact]
    public void Confirmation_timeout_does_not_confirm_and_allows_retry()
    {
        var rule = new MacroRule();
        var controller = AwaitingCooldown(rule);

        Assert.Equal(ReleaseDecision.TimedOut, controller.Observe(rule, IconVisualState.Ready, Now.AddMilliseconds(801)));
        Assert.Equal(ReleaseDecision.None, controller.Observe(rule, IconVisualState.Ready, Now.AddMilliseconds(900)));
        Assert.Equal(ReleaseDecision.SendKey, controller.Observe(rule, IconVisualState.Ready, Now.AddMilliseconds(950)));
    }

    [Fact]
    public void Confirmation_window_includes_cast_time()
    {
        var rule = new MacroRule { CastTimeSeconds = 1.8 };
        var controller = AwaitingCooldown(rule);

        Assert.Equal(ReleaseDecision.None, controller.Observe(rule, IconVisualState.Cooldown, Now.AddSeconds(2)));
        Assert.Equal(ReleaseDecision.Confirmed, controller.Observe(rule, IconVisualState.Cooldown, Now.AddSeconds(2.05)));
    }

    [Fact]
    public void Confirmation_window_uses_rule_network_margin()
    {
        var rule = new MacroRule { NetworkMarginMs = 1200 };
        var controller = AwaitingCooldown(rule);

        Assert.Equal(ReleaseDecision.None, controller.Observe(rule, IconVisualState.Ready, Now.AddMilliseconds(1000)));
        Assert.Equal(ReleaseDecision.TimedOut, controller.Observe(rule, IconVisualState.Ready, Now.AddMilliseconds(1201)));
    }

    private static ClosedLoopReleaseController AwaitingCooldown(MacroRule rule)
    {
        var controller = new ClosedLoopReleaseController();
        controller.Observe(rule, IconVisualState.Ready, Now);
        controller.Observe(rule, IconVisualState.Ready, Now);
        return controller;
    }
}
