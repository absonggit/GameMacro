using GameMacro.Core.Models;
using GameMacro.Core.Runtime;

namespace GameMacro.Core.Tests.Runtime;

public sealed class MacroEngineTests
{
    [Fact]
    public async Task Tick_sends_only_highest_priority_ready_rule()
    {
        var fixture = EngineFixture.Create("F1", "F2");

        await fixture.Engine.TickAsync(CancellationToken.None);

        Assert.Equal(["F1"], fixture.Input.Keys);
    }

    [Fact]
    public async Task Tick_sends_nothing_when_target_is_not_foreground()
    {
        var fixture = EngineFixture.Create("F1");
        fixture.Window.IsForeground = false;

        await fixture.Engine.TickAsync(CancellationToken.None);

        Assert.Empty(fixture.Input.Keys);
    }

    [Fact]
    public async Task Tick_honors_rule_protection_period()
    {
        var fixture = EngineFixture.Create("F1");

        await fixture.Engine.TickAsync(CancellationToken.None);
        await fixture.Engine.TickAsync(CancellationToken.None);

        Assert.Equal(["F1"], fixture.Input.Keys);
    }

    [Fact]
    public async Task Stop_releases_all_held_inputs()
    {
        var fixture = EngineFixture.Create();

        await fixture.Engine.StopAsync();

        Assert.Equal(1, fixture.Input.ReleaseAllCount);
    }

    private sealed class EngineFixture
    {
        public required MacroEngine Engine { get; init; }
        public required FakeWindowGate Window { get; init; }
        public required FakeInputSink Input { get; init; }

        public static EngineFixture Create(params string[] readyKeys)
        {
            var window = new FakeWindowGate();
            var input = new FakeInputSink();
            var profile = new MacroProfile
            {
                Rules = readyKeys.Select((key, index) => new MacroRule
                {
                    Name = key,
                    ActionKey = key,
                    Priority = index + 1,
                    Mode = RuleMode.Conditional,
                    ProtectionMs = 300
                }).ToList()
            };
            var engine = new MacroEngine(profile, window, new AlwaysReadyEvaluator(), input, new FakeClock());
            return new() { Engine = engine, Window = window, Input = input };
        }
    }

    private sealed class FakeWindowGate : IWindowGate
    {
        public bool IsForeground { get; set; } = true;
        public ValueTask<bool> IsTargetForegroundAsync(MacroProfile profile, CancellationToken cancellationToken)
            => ValueTask.FromResult(IsForeground);
    }

    private sealed class AlwaysReadyEvaluator : IConditionEvaluator
    {
        public ValueTask<bool> IsReadyAsync(MacroRule rule, CancellationToken cancellationToken)
            => ValueTask.FromResult(true);
    }

    private sealed class FakeInputSink : IInputSink
    {
        public List<string> Keys { get; } = [];
        public int ReleaseAllCount { get; private set; }

        public ValueTask EnqueueAsync(string actionKey, CancellationToken cancellationToken)
        {
            Keys.Add(actionKey);
            return ValueTask.CompletedTask;
        }

        public ValueTask ReleaseAllAsync()
        {
            ReleaseAllCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 7, 12, 0, 0, 0, TimeSpan.Zero);
        public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }
}
