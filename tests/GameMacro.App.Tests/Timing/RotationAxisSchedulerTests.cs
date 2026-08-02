using GameMacro.App.Detection;
using GameMacro.App.Timing;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Timing;

public sealed class RotationAxisSchedulerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Burst_no_cooldown_step_waits_200ms_then_runs_five_times_at_1000ms_intervals()
    {
        var scheduler = new RotationAxisScheduler();
        var f4 = Rule(1);
        var four = Rule(2);
        four.BurstRepeatCount = 5;
        four.BurstInitialDelayMs = 200;
        four.BurstRepeatIntervalMs = 1000;
        four.AllowNoCooldownBurstRepeat = true;
        var profile = new MacroProfile { Rules = [f4, four], BurstAxisRuleIds = [f4.Id, four.Id] };
        var states = States((f4, IconVisualState.Ready), (four, IconVisualState.Ready));

        Assert.Same(f4, scheduler.Select(profile, states, Now));
        scheduler.RecordReleased(f4, Now);
        Assert.Null(scheduler.Select(profile, states, Now.AddMilliseconds(199)));
        for (var repeat = 0; repeat < 5; repeat++)
        {
            var due = Now.AddMilliseconds(200 + repeat * 1000);
            Assert.Same(four, scheduler.Select(profile, states, due));
            scheduler.RecordReleased(four, due);
            if (repeat < 4) Assert.Null(scheduler.Select(profile, states, due.AddMilliseconds(999)));
        }
        Assert.Null(scheduler.Select(profile, states, Now.AddMilliseconds(4201)));
    }

    [Fact]
    public void No_cooldown_repeat_only_needs_png_ready_for_first_send()
    {
        var scheduler = new RotationAxisScheduler();
        var four = Rule(1);
        four.BurstRepeatCount = 2;
        four.BurstRepeatIntervalMs = 1000;
        four.AllowNoCooldownBurstRepeat = true;
        var profile = new MacroProfile { Rules = [four], BurstAxisRuleIds = [four.Id] };

        Assert.Same(four, scheduler.Select(profile, States((four, IconVisualState.Ready)), Now));
        scheduler.RecordReleased(four, Now);

        Assert.Same(four, scheduler.Select(profile, States((four, IconVisualState.Cooldown)), Now.AddMilliseconds(1000)));
    }

    [Fact]
    public void Explicit_burst_group_requires_every_member_and_uses_group_order()
    {
        var scheduler = new RotationAxisScheduler();
        var f1 = Rule(1);
        var f2 = Rule(2);
        var profile = new MacroProfile { Rules = [f1, f2], BurstAxisRuleIds = [f2.Id, f1.Id] };

        Assert.Null(scheduler.Select(profile, States((f1, IconVisualState.Ready), (f2, IconVisualState.Cooldown))));
        Assert.Same(f2, scheduler.Select(profile, States((f1, IconVisualState.Ready), (f2, IconVisualState.Ready))));
    }

    [Fact]
    public void Explicit_base_group_scans_from_left_on_every_selection()
    {
        var scheduler = new RotationAxisScheduler();
        var left = Rule(1);
        var right = Rule(2);
        var profile = new MacroProfile { Rules = [left, right], BasePriorityRuleIds = [left.Id, right.Id] };

        Assert.Same(right, scheduler.Select(profile, States((left, IconVisualState.Cooldown), (right, IconVisualState.Ready))));
        scheduler.RecordReleased(right);
        Assert.Same(left, scheduler.Select(profile, States((left, IconVisualState.Ready), (right, IconVisualState.Cooldown))));
    }

    [Fact]
    public void Burst_waits_until_every_member_is_ready_then_keeps_axis_order()
    {
        var scheduler = new RotationAxisScheduler();
        var f1 = Rule(1, burst: true);
        var f2 = Rule(2, burst: true);
        var rules = new[] { f1, f2 };

        Assert.Null(scheduler.Select(rules, States((f1, IconVisualState.Ready), (f2, IconVisualState.Cooldown))));
        Assert.Same(f1, scheduler.Select(rules, States((f1, IconVisualState.Ready), (f2, IconVisualState.Ready))));
        scheduler.RecordReleased(f1);
        Assert.Same(f2, scheduler.Select(rules, States((f1, IconVisualState.Cooldown), (f2, IconVisualState.Ready))));
    }

    [Fact]
    public void Active_burst_waits_for_its_next_member_instead_of_using_filler()
    {
        var scheduler = new RotationAxisScheduler();
        var f1 = Rule(1, burst: true);
        var f2 = Rule(2, burst: true);
        var filler = Rule(3, filler: true);
        var rules = new[] { f1, f2, filler };
        Assert.Same(f1, scheduler.Select(rules, States((f1, IconVisualState.Ready), (f2, IconVisualState.Ready), (filler, IconVisualState.Ready))));
        scheduler.RecordReleased(f1);

        Assert.Null(scheduler.Select(rules, States((f1, IconVisualState.Cooldown), (f2, IconVisualState.Cooldown), (filler, IconVisualState.Ready))));
    }

    [Fact]
    public void Ready_insert_skill_has_priority_over_filler()
    {
        var scheduler = new RotationAxisScheduler();
        var insert = Rule(1, insert: true);
        var filler = Rule(2, filler: true);

        var selected = scheduler.Select([insert, filler], States((insert, IconVisualState.Ready), (filler, IconVisualState.Ready)));

        Assert.Same(insert, selected);
    }

    [Fact]
    public void Filler_continues_from_next_position_after_insert()
    {
        var scheduler = new RotationAxisScheduler();
        var insert = Rule(1, insert: true);
        var one = Rule(2, filler: true);
        var two = Rule(3, filler: true);
        var rules = new[] { insert, one, two };
        Assert.Same(one, scheduler.Select(rules, States((insert, IconVisualState.Cooldown), (one, IconVisualState.Ready), (two, IconVisualState.Ready))));
        scheduler.RecordReleased(one);
        Assert.Same(insert, scheduler.Select(rules, States((insert, IconVisualState.Ready), (one, IconVisualState.Cooldown), (two, IconVisualState.Ready))));
        scheduler.RecordReleased(insert);

        Assert.Same(two, scheduler.Select(rules, States((insert, IconVisualState.Cooldown), (one, IconVisualState.Cooldown), (two, IconVisualState.Ready))));
    }

    [Fact]
    public void Released_skill_cannot_repeat_until_png_leaves_ready_state()
    {
        var scheduler = new RotationAxisScheduler();
        var insert = Rule(1, insert: true);
        scheduler.RecordReleased(insert);

        Assert.Null(scheduler.Select([insert], States((insert, IconVisualState.Ready))));
        Assert.Null(scheduler.Select([insert], States((insert, IconVisualState.Cooldown))));
        Assert.Same(insert, scheduler.Select([insert], States((insert, IconVisualState.Ready))));
    }

    private static MacroRule Rule(int priority, bool burst = false, bool filler = false, bool insert = false) => new()
    {
        Priority = priority,
        IsBurstAxisMember = burst,
        IsFillerAxisMember = filler,
        IsShortCooldownInsert = insert
    };

    private static IReadOnlyDictionary<Guid, IconVisualState> States(params (MacroRule Rule, IconVisualState State)[] values)
        => values.ToDictionary(value => value.Rule.Id, value => value.State);
}
