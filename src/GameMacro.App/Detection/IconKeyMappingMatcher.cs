using GameMacro.Core.Models;

namespace GameMacro.App.Detection;

public sealed record IconMatchResult(IconKeyMapping Mapping, double Distance);

public static class IconKeyMappingMatcher
{
    public static IconMatchResult? Match(double[] sample, IEnumerable<IconKeyMapping> mappings)
    {
        if (sample.Length == 0) return null;

        var candidates = mappings
            .Where(mapping => mapping.Enabled
                && mapping.IsCalibrated
                && mapping.Signature.Length == sample.Length)
            .Select(mapping => new IconMatchResult(mapping,
                IconVisualSignature.Distance(sample, mapping.Signature)))
            .Where(result => double.IsFinite(result.Distance))
            .OrderBy(result => result.Distance)
            .ThenBy(result => result.Mapping.Id)
            .Take(2)
            .ToList();
        if (candidates.Count == 0) return null;

        var closest = candidates[0];
        if (IconVisualSignature.IsCurrent(sample))
            return IconVisualSignature.IsLikelyEmpty(sample) ? null : closest;
        if (closest.Distance <= closest.Mapping.MatchThreshold) return closest;
        if (candidates.Count < 2 || candidates[1].Distance <= 0) return null;

        // The same icon is rendered with different borders, scale and key labels in the
        // dynamic slot and in the skill source bar. Absolute distance can therefore be
        // high, while the correct template remains clearly closer than every alternative.
        return closest.Distance / candidates[1].Distance <= .82 ? closest : null;
    }
}
