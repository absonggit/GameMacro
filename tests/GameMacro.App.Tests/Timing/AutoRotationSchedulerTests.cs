using GameMacro.App.Timing;
using GameMacro.App.Detection;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Timing;

public sealed class AutoRotationSchedulerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Selects_highest_priority_ready_skill()
    {
        var scheduler = new AutoRotationScheduler(new HybridCooldownTracker());
        var low = new MacroRule { ActionKey = "F2", Priority = 2 };
        var high = new MacroRule { ActionKey = "F1", Priority = 1 };

        var selected = scheduler.TrySelect([low, high], Now);

        Assert.Same(high, selected);
    }

    [Fact]
    public void Skips_skill_that_is_still_on_cooldown()
    {
        var tracker = new HybridCooldownTracker();
        var scheduler = new AutoRotationScheduler(tracker);
        var first = new MacroRule { ActionKey = "F1", Priority = 1, BaseCooldownSeconds = 10 };
        var second = new MacroRule { ActionKey = "F2", Priority = 2 };
        scheduler.RecordSuccessfulCast(first, Now);

        var selected = scheduler.TrySelect([first, second], Now.AddSeconds(1));

        Assert.Same(second, selected);
    }

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(1.8, 2.8)]
    public void Successful_cast_locks_scheduler_for_cast_time_plus_one_second_gcd(double castSeconds, double expectedLock)
    {
        var scheduler = new AutoRotationScheduler(new HybridCooldownTracker());
        var rule = new MacroRule { CastTimeSeconds = castSeconds };

        scheduler.RecordSuccessfulCast(rule, Now);

        Assert.True(scheduler.IsGloballyLocked(Now.AddSeconds(expectedLock - .01)));
        Assert.False(scheduler.IsGloballyLocked(Now.AddSeconds(expectedLock)));
    }

    [Fact]
    public void Delayed_visual_confirmation_does_not_restart_cast_lock()
    {
        var scheduler = new AutoRotationScheduler(new HybridCooldownTracker());
        var rule = new MacroRule { CastTimeSeconds = 1.8 };

        scheduler.RecordConfirmedCast(rule, Now.AddSeconds(2), Now);

        Assert.True(scheduler.IsGloballyLocked(Now.AddSeconds(2.79)));
        Assert.False(scheduler.IsGloballyLocked(Now.AddSeconds(2.8)));
    }

    [Fact]
    public void Visual_selection_skips_unknown_high_priority_and_uses_next_ready_skill()
    {
        var scheduler = new AutoRotationScheduler(new HybridCooldownTracker());
        var first = new MacroRule { Priority = 1 };
        var second = new MacroRule { Priority = 2 };

        var selected = scheduler.TrySelectVisual([first, second], Now,
            rule => ReferenceEquals(rule, first) ? IconVisualState.Unknown : IconVisualState.Ready);

        Assert.Same(second, selected);
    }

    [Fact]
    public void Visual_selection_ignores_local_cooldown_tracker()
    {
        var tracker = new HybridCooldownTracker();
        var scheduler = new AutoRotationScheduler(tracker);
        var rule = new MacroRule { BaseCooldownSeconds = 60 };
        tracker.OnPhysicalKey(rule, Now);

        var selected = scheduler.TrySelectVisual([rule], Now.AddSeconds(1), _ => IconVisualState.Ready);

        Assert.Same(rule, selected);
    }

    [Fact]
    public void Successful_key_does_not_block_visual_selection()
    {
        var scheduler = new AutoRotationScheduler(new HybridCooldownTracker());
        var rule = new MacroRule { Priority = 1 };

        scheduler.RecordKeySent(Now);
        var selected = scheduler.TrySelectVisual([rule], Now, _ => IconVisualState.Ready);

        Assert.Same(rule, selected);
    }
}
