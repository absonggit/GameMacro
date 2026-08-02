using System.Runtime.InteropServices;
using GameMacro.Core.Runtime;

namespace GameMacro.App.Platform;

public sealed class SendInputService : IInputSink
{
    private readonly KeyPulseSender _pulseSender = new(Send);

    public async ValueTask EnqueueAsync(string actionKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var virtualKey = VirtualKeyParser.Parse(actionKey)
            ?? throw new ArgumentException($"暂不支持按键 {actionKey}。", nameof(actionKey));
        await _pulseSender.SendAsync(virtualKey, cancellationToken);
    }

    public ValueTask ReleaseAllAsync() => ValueTask.CompletedTask;

    private static void Send(ushort virtualKey, bool keyUp)
    {
        NativeMethods.Input[] inputs =
        [
            new()
            {
                Type = NativeMethods.InputKeyboard,
                Union = new NativeMethods.InputUnion
                {
                    Keyboard = new NativeMethods.KeyboardInput
                    {
                        VirtualKey = virtualKey,
                        Flags = keyUp ? NativeMethods.KeyUp : 0
                    }
                }
            }
        ];
        if (NativeMethods.SendInput(1, inputs, SendInputLayout.Size) != 1)
            throw new InvalidOperationException("Windows 未能发送按键。");
    }
}
