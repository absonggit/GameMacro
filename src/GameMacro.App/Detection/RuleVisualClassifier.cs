using GameMacro.Core.Models;

namespace GameMacro.App.Detection;

public static class RuleVisualClassifier
{
    public static IconVisualState Classify(double[] sample, MacroRule rule)
    {
        List<IconVisualState> states =
        [
            IconStateClassifier.Classify(sample, rule.ReadySignature, rule.ReadyThreshold, rule.ChangeThreshold)
        ];
        states.AddRange(rule.AdditionalReadyIcons.Select(template => IconStateClassifier.Classify(
            sample, template.Signature, template.ReadyThreshold, template.ChangeThreshold)));
        if (states.Contains(IconVisualState.Ready)) return IconVisualState.Ready;
        return states.All(state => state == IconVisualState.Cooldown)
            ? IconVisualState.Cooldown
            : IconVisualState.Unknown;
    }
}
