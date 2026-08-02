using GameMacro.App.Overlay;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Overlay;

public sealed class OverlayProfilePolicyTests
{
    [Fact]
    public void ProfilesForTarget_prefers_process_name_case_insensitively()
    {
        var current = Profile("当前", "ZhuxianClient-Win64-Shipping", "诛仙世界");
        var same = Profile("同进程", "zhuxianclient-win64-shipping", "另一个标题");
        var other = Profile("其他", "OtherGame", "诛仙世界");

        var result = OverlayProfilePolicy.ProfilesForTarget([current, same, other], current);

        Assert.Equal([current, same], result);
    }

    [Fact]
    public void ProfilesForTarget_falls_back_to_trimmed_title_when_process_is_empty()
    {
        var current = Profile("当前", "", " 诛仙世界 ");
        var same = Profile("同标题", "", "诛仙世界");
        var other = Profile("其他", "", "其他游戏");

        var result = OverlayProfilePolicy.ProfilesForTarget([current, same, other], current);

        Assert.Equal([current, same], result);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void CanSwitch_only_when_stopped(bool isRunning, bool expected)
    {
        Assert.Equal(expected, OverlayProfilePolicy.CanSwitch(isRunning));
    }

    private static MacroProfile Profile(string name, string process, string title) => new()
    {
        Name = name,
        TargetProcessName = process,
        TargetWindowTitle = title,
    };
}
