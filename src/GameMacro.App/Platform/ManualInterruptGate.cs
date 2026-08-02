namespace GameMacro.App.Platform;

public sealed class ManualInterruptGate(
    Func<DateTimeOffset>? clock = null,
    TimeSpan? resumeDelay = null)
{
    private readonly Func<DateTimeOffset> _clock = clock ?? (() => DateTimeOffset.UtcNow);
    private readonly TimeSpan _resumeDelay = resumeDelay ?? TimeSpan.FromSeconds(1);
    private readonly HashSet<ushort> _heldKeys = [];
    private DateTimeOffset? _resumeAt;

    public bool IsPaused => _heldKeys.Count > 0 || _resumeAt is { } deadline && _clock() < deadline;

    public bool IsHeld(ushort virtualKey) => _heldKeys.Contains(virtualKey);

    public void KeyDown(ushort virtualKey)
    {
        _heldKeys.Add(virtualKey);
        _resumeAt = null;
    }

    public void KeyUp(ushort virtualKey)
    {
        if (!_heldKeys.Remove(virtualKey)) return;
        if (_heldKeys.Count == 0) _resumeAt = _clock() + _resumeDelay;
    }

    public void Reset()
    {
        _heldKeys.Clear();
        _resumeAt = null;
    }
}
