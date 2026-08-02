using GameMacro.App.Platform;

namespace GameMacro.App.Tests.Platform;

public sealed class ManualInterruptGateTests
{
    [Fact]
    public void Key_down_pauses_immediately()
    {
        var gate = new ManualInterruptGate();

        gate.KeyDown(0x31);

        Assert.True(gate.IsPaused);
        Assert.True(gate.IsHeld(0x31));
    }

    [Fact]
    public void Last_key_up_starts_one_second_resume_delay()
    {
        var now = DateTimeOffset.Parse("2026-08-02T00:00:00Z");
        var gate = new ManualInterruptGate(() => now);
        gate.KeyDown(0x31);
        gate.KeyDown(0x32);

        gate.KeyUp(0x31);
        Assert.True(gate.IsPaused);
        gate.KeyUp(0x32);
        now += TimeSpan.FromMilliseconds(999);
        Assert.True(gate.IsPaused);
        now += TimeSpan.FromMilliseconds(1);
        Assert.False(gate.IsPaused);
    }

    [Fact]
    public void Repeated_key_down_needs_only_one_key_up()
    {
        var now = DateTimeOffset.Parse("2026-08-02T00:00:00Z");
        var gate = new ManualInterruptGate(() => now);
        gate.KeyDown(0x31);
        gate.KeyDown(0x31);

        gate.KeyUp(0x31);
        now += TimeSpan.FromSeconds(1);

        Assert.False(gate.IsPaused);
    }

    [Fact]
    public void New_key_down_during_resume_delay_cancels_the_deadline()
    {
        var now = DateTimeOffset.Parse("2026-08-02T00:00:00Z");
        var gate = new ManualInterruptGate(() => now);
        gate.KeyDown(0x31);
        gate.KeyUp(0x31);
        now += TimeSpan.FromMilliseconds(900);

        gate.KeyDown(0x32);
        now += TimeSpan.FromSeconds(2);

        Assert.True(gate.IsPaused);
        Assert.True(gate.IsHeld(0x32));
    }

    [Fact]
    public void Unknown_key_up_does_not_start_a_resume_delay()
    {
        var gate = new ManualInterruptGate();

        gate.KeyUp(0x31);

        Assert.False(gate.IsPaused);
    }

    [Fact]
    public void Reset_clears_held_keys_and_resume_deadline()
    {
        var gate = new ManualInterruptGate();
        gate.KeyDown(0x31);
        gate.Reset();
        Assert.False(gate.IsPaused);

        gate.KeyDown(0x32);
        gate.KeyUp(0x32);
        gate.Reset();

        Assert.False(gate.IsPaused);
        Assert.False(gate.IsHeld(0x31));
        Assert.False(gate.IsHeld(0x32));
    }
}
