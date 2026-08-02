using GameMacro.App.Detection;
using GameMacro.Core.Models;

namespace GameMacro.App.Tests.Detection;

public sealed class RuleVisualClassifierTests
{
    [Fact]
    public void Any_saved_ready_icon_can_make_same_skill_available()
    {
        var rule = new MacroRule
        {
            ReadySignature = [0], ReadyThreshold = .1, ChangeThreshold = .4,
            AdditionalReadyIcons = [new() { Signature = [1], ReadyThreshold = .1, ChangeThreshold = .4 }]
        };

        var state = RuleVisualClassifier.Classify([1.02], rule);

        Assert.Equal(IconVisualState.Ready, state);
    }
}
