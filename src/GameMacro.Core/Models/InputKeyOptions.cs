namespace GameMacro.Core.Models;

public static class InputKeyOptions
{
    public static IReadOnlyList<string> All { get; } =
        Enumerable.Range(1, 12).Select(value => $"F{value}")
            .Concat(Enumerable.Range(0, 10).Select(value => value.ToString()))
            .Concat(Enumerable.Range('A', 26).Select(value => ((char)value).ToString()))
            .Append("~")
            .ToArray();
}
