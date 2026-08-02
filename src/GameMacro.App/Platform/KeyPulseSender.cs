namespace GameMacro.App.Platform;

public sealed class KeyPulseSender(
    Action<ushort, bool> send,
    Func<TimeSpan, CancellationToken, Task>? delay = null,
    TimeSpan? holdDuration = null)
{
    private readonly Func<TimeSpan, CancellationToken, Task> _delay = delay ?? Task.Delay;
    private readonly TimeSpan _holdDuration = holdDuration ?? TimeSpan.FromMilliseconds(12);

    public async ValueTask SendAsync(ushort virtualKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        send(virtualKey, false);
        try
        {
            await _delay(_holdDuration, cancellationToken);
        }
        finally
        {
            send(virtualKey, true);
        }
    }
}
