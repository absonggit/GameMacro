namespace GameMacro.App.Platform;

public sealed class ManualInterruptRouter(ManualInterruptGate gate)
{
    public void Handle(
        PhysicalKeyboardEventArgs value,
        bool automationRunning,
        bool targetForeground,
        IReadOnlySet<ushort> configuredKeys)
    {
        if (!value.IsDown)
        {
            if (gate.IsHeld(value.VirtualKey)) gate.KeyUp(value.VirtualKey);
            return;
        }
        if (automationRunning && targetForeground && configuredKeys.Contains(value.VirtualKey))
            gate.KeyDown(value.VirtualKey);
    }
}
