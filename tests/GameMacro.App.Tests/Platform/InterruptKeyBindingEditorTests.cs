using GameMacro.App.Platform;

namespace GameMacro.App.Tests.Platform;

public sealed class InterruptKeyBindingEditorTests
{
    [Fact]
    public void Supported_key_is_added()
    {
        List<string> keys = [];

        var accepted = InterruptKeyBindingEditor.TryAdd(keys, "Q", "F5", out var error);

        Assert.True(accepted);
        Assert.Null(error);
        Assert.Equal(["Q"], keys);
    }

    [Fact]
    public void Duplicate_key_is_idempotent_case_insensitively()
    {
        List<string> keys = ["Q"];

        var accepted = InterruptKeyBindingEditor.TryAdd(keys, "q", "F5", out var error);

        Assert.True(accepted);
        Assert.Null(error);
        Assert.Equal(["Q"], keys);
    }

    [Fact]
    public void Toggle_hotkey_is_rejected()
    {
        List<string> keys = [];

        var accepted = InterruptKeyBindingEditor.TryAdd(keys, "F5", "F5", out var error);

        Assert.False(accepted);
        Assert.Contains("启停热键", error);
        Assert.Empty(keys);
    }

    [Fact]
    public void Unsupported_key_is_rejected()
    {
        List<string> keys = [];

        var accepted = InterruptKeyBindingEditor.TryAdd(keys, "Space", "F5", out var error);

        Assert.False(accepted);
        Assert.Contains("不受支持", error);
        Assert.Empty(keys);
    }
}
