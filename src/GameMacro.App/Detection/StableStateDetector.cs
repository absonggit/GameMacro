namespace GameMacro.App.Detection;

public sealed class StableStateDetector(int requiredSamples)
{
    private IconVisualState _candidate;
    private int _count;
    public IconVisualState ConfirmedState { get; private set; } = IconVisualState.Unknown;

    public bool Observe(IconVisualState state)
    {
        if (state == IconVisualState.Unknown)
        {
            _candidate = IconVisualState.Unknown;
            _count = 0;
            return false;
        }
        if (_candidate != state) { _candidate = state; _count = 0; }
        _count++;
        if (_count < Math.Max(1, requiredSamples)) return false;
        ConfirmedState = state;
        return true;
    }

    public void Reset()
    {
        _candidate = IconVisualState.Unknown;
        _count = 0;
        ConfirmedState = IconVisualState.Unknown;
    }
}
