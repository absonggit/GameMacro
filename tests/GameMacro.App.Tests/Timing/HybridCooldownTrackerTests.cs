using GameMacro.App.Timing;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Timing;

public sealed class HybridCooldownTrackerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Physical_key_starts_cooldown_and_cast_concurrently()
    {
        var rule = new MacroRule { BaseCooldownSeconds = 10, CastTimeSeconds = 1.5 };
        var tracker = new HybridCooldownTracker();

        tracker.OnPhysicalKey(rule, Now);

        var immediate = tracker.GetSnapshot(rule, Now);
        Assert.Equal(HybridSkillState.Casting, immediate.State);
        Assert.Equal(10, immediate.RemainingSeconds);
        var later = tracker.GetSnapshot(rule, Now.AddSeconds(2));
        Assert.Equal(HybridSkillState.Cooldown, later.State);
        Assert.Equal(8, later.RemainingSeconds);
    }

}
