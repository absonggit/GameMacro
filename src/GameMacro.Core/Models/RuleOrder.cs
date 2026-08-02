namespace GameMacro.Core.Models;

public static class RuleOrder
{
    public static void Move(List<MacroRule> rules, MacroRule source, MacroRule target)
    {
        if (ReferenceEquals(source, target) || !rules.Remove(source)) return;
        var targetIndex = rules.IndexOf(target);
        rules.Insert(targetIndex < 0 ? rules.Count : targetIndex + 1, source);
        Renumber(rules);
    }

    public static void Renumber(IReadOnlyList<MacroRule> rules)
    {
        for (var index = 0; index < rules.Count; index++) rules[index].Priority = index + 1;
    }
}
