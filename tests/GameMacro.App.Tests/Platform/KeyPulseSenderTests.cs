using GameMacro.App.Platform;

namespace GameMacro.App.Tests.Platform;

public sealed class KeyPulseSenderTests
{
    [Fact]
    public async Task Sends_key_down_immediately_and_key_up_after_hold_period()
    {
        List<(ushort Key, bool IsUp)> events = [];
        var releaseDelay = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sender = new KeyPulseSender(
            (key, isUp) => events.Add((key, isUp)),
            (_, _) => releaseDelay.Task,
            TimeSpan.FromMilliseconds(12));

        var sending = sender.SendAsync(0x70, CancellationToken.None).AsTask();

        Assert.Equal([(0x70, false)], events);
        releaseDelay.SetResult();
        await sending;
        Assert.Equal([(0x70, false), (0x70, true)], events);
    }
}
