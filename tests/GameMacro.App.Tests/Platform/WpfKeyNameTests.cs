using System.Windows.Input;
using GameMacro.App.Platform;

namespace GameMacro.App.Tests.Platform;

public sealed class WpfKeyNameTests
{
    [Theory]
    [InlineData(Key.F5, "F5")]
    [InlineData(Key.D7, "7")]
    [InlineData(Key.NumPad3, "3")]
    [InlineData(Key.C, "C")]
    [InlineData(Key.Oem3, "~")]
    public void FromKey_returns_supported_binding_name(Key key, string expected)
    {
        Assert.Equal(expected, WpfKeyName.FromKey(key));
    }

    [Fact]
    public void FromKey_returns_null_for_unsupported_key()
    {
        Assert.Null(WpfKeyName.FromKey(Key.LeftShift));
    }
}
