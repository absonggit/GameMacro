using GameMacro.Core.Models;

namespace GameMacro.Core.Runtime;

public interface IWindowGate
{
    ValueTask<bool> IsTargetForegroundAsync(MacroProfile profile, CancellationToken cancellationToken);
}

public interface IConditionEvaluator
{
    ValueTask<bool> IsReadyAsync(MacroRule rule, CancellationToken cancellationToken);
}

public interface IInputSink
{
    ValueTask EnqueueAsync(string actionKey, CancellationToken cancellationToken);
    ValueTask ReleaseAllAsync();
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }
    ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public async ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        => await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
}

public sealed record EngineStatus(bool IsRunning, string Message, string? LastTriggeredKey = null);
