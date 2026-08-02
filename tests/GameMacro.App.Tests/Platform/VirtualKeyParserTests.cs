using GameMacro.App.Platform;

namespace GameMacro.App.Tests.Platform;

public sealed class VirtualKeyParserTests
{
    [Fact]
    public void Parses_tilde_as_oem3() => Assert.Equal((ushort)0xC0, VirtualKeyParser.Parse("~"));
    [Theory]
    [InlineData("F1", 0x70)]
    [InlineData("F12", 0x7B)]
    [InlineData("a", 0x41)]
    [InlineData("9", 0x39)]
    [InlineData("F25", null)]
    [InlineData("Spacebar", null)]
    public void Parse_maps_supported_keys(string text, int? expected)
        => Assert.Equal(expected, VirtualKeyParser.Parse(text));
}
