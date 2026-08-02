using GameMacro.App.Platform;

namespace GameMacro.App.Tests.Platform;

public sealed class PhysicalKeyboardEventDecoderTests
{
    [Theory]
    [InlineData(0x0100, true)]
    [InlineData(0x0104, true)]
    [InlineData(0x0101, false)]
    [InlineData(0x0105, false)]
    public void Physical_keyboard_messages_are_decoded(int message, bool isDown)
    {
        var decoded = PhysicalKeyboardEventDecoder.TryDecode(0, message, 0x31, 0, out var value);

        Assert.True(decoded);
        Assert.NotNull(value);
        Assert.Equal((0x31, isDown), (value.VirtualKey, value.IsDown));
    }

    [Fact]
    public void Injected_input_is_ignored()
    {
        Assert.False(PhysicalKeyboardEventDecoder.TryDecode(0, 0x0100, 0x31, 0x10, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Negative_hook_code_is_ignored()
    {
        Assert.False(PhysicalKeyboardEventDecoder.TryDecode(-1, 0x0100, 0x31, 0, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Unrelated_window_message_is_ignored()
    {
        Assert.False(PhysicalKeyboardEventDecoder.TryDecode(0, 0x0201, 0x31, 0, out var value));
        Assert.Null(value);
    }
}
