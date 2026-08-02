using GameMacro.Core.Models;

namespace GameMacro.Core.Runtime;

public sealed class MacroEngine(
    MacroProfile profile,
    IWindowGate windowGate,
    IConditionEvaluator conditionEvaluator,
    IInputSink inputSink,
    IClock clock)
{
    private readonly Dictionary<Guid, DateTimeOffset> _lastTriggered = [];
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private CancellationTokenSource? _loopCancellation;
    private Task? _loopTask;

    public event EventHandler<EngineStatus>? StatusChanged;
    public bool IsRunning => _loopTask is { IsCompleted: false };

    public async Task TickAsync(CancellationToken cancellationToken)
    {
        if (!await windowGate.IsTargetForegroundAsync(profile, cancellationToken))
        {
            StatusChanged?.Invoke(this, new(false, "目标窗口不在前台，已暂停。"));
            return;
        }

        foreach (var rule in profile.Rules.Where(rule => rule.Enabled && rule.Mode == RuleMode.FixedInterval))
        {
            if (CanTrigger(rule))
                await TriggerAsync(rule, cancellationToken);
        }

        foreach (var rule in profile.OrderedConditionalRules())
        {
            if (!CanTrigger(rule))
                continue;
            if (!await conditionEvaluator.IsReadyAsync(rule, cancellationToken))
                continue;

            await TriggerAsync(rule, cancellationToken);
            break;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            if (IsRunning)
                return;
            _loopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _loopTask = RunLoopAsync(_loopCancellation.Token);
            StatusChanged?.Invoke(this, new(true, "运行中"));
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task StopAsync()
    {
        await _lifecycleLock.WaitAsync();
        try
        {
            _loopCancellation?.Cancel();
            if (_loopTask is not null)
            {
                try { await _loopTask; }
                catch (OperationCanceledException) { }
            }
            else
            {
                await inputSink.ReleaseAllAsync();
            }
            _loopTask = null;
            _loopCancellation?.Dispose();
            _loopCancellation = null;
            StatusChanged?.Invoke(this, new(false, "已停止"));
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await TickAsync(cancellationToken);
                await clock.DelayAsync(TimeSpan.FromMilliseconds(50), cancellationToken);
            }
        }
        finally
        {
            await inputSink.ReleaseAllAsync();
        }
    }

    private bool CanTrigger(MacroRule rule)
    {
        if (!_lastTriggered.TryGetValue(rule.Id, out var last))
            return true;
        var requiredDelay = rule.Mode == RuleMode.FixedInterval ? rule.IntervalMs : rule.ProtectionMs;
        return clock.UtcNow - last >= TimeSpan.FromMilliseconds(requiredDelay);
    }

    private async Task TriggerAsync(MacroRule rule, CancellationToken cancellationToken)
    {
        await inputSink.EnqueueAsync(rule.ActionKey, cancellationToken);
        _lastTriggered[rule.Id] = clock.UtcNow;
        StatusChanged?.Invoke(this, new(true, "运行中", rule.ActionKey));
    }
}
