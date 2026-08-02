using GameMacro.App.Platform;

namespace GameMacro.App.Tests.Platform;

public sealed class ManualInterruptRouterTests
{
    [Theory]
    [InlineData(false, true, 0x31)]
    [InlineData(true, false, 0x31)]
    [InlineData(true, true, 0x32)]
    public void Key_down_is_ignored_unless_running_foreground_and_configured(
        bool running,
        bool foreground,
        ushort virtualKey)
    {
        var gate = new ManualInterruptGate();
        var router = new ManualInterruptRouter(gate);

        router.Handle(
            new PhysicalKeyboardEventArgs(virtualKey, true),
            running,
            foreground,
            new HashSet<ushort> { 0x31 });

        Assert.False(gate.IsPaused);
    }

    [Fact]
    public void Configured_foreground_key_down_pauses()
    {
        var gate = new ManualInterruptGate();
        var router = new ManualInterruptRouter(gate);

        router.Handle(
            new PhysicalKeyboardEventArgs(0x31, true),
            true,
            true,
            new HashSet<ushort> { 0x31 });

        Assert.True(gate.IsPaused);
        Assert.True(gate.IsHeld(0x31));
    }

    [Fact]
    public void Tracked_key_up_is_processed_after_target_loses_foreground()
    {
        var now = DateTimeOffset.Parse("2026-08-02T00:00:00Z");
        var gate = new ManualInterruptGate(() => now);
        var router = new ManualInterruptRouter(gate);
        router.Handle(new PhysicalKeyboardEventArgs(0x31, true), true, true, new HashSet<ushort> { 0x31 });

        router.Handle(new PhysicalKeyboardEventArgs(0x31, false), true, false, new HashSet<ushort> { 0x31 });
        now += TimeSpan.FromSeconds(1);

        Assert.False(gate.IsPaused);
    }
}
