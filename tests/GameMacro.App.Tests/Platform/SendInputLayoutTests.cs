using GameMacro.App.Platform;

namespace GameMacro.App.Tests.Platform;

public sealed class SendInputLayoutTests
{
    [Fact]
    public void Input_structure_matches_native_windows_size()
        => Assert.Equal(IntPtr.Size == 8 ? 40 : 28, SendInputLayout.Size);
}
