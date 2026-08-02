using GameMacro.Core.Models;

namespace GameMacro.App.Detection;

public sealed class DynamicIconRecognizer(
    Func<double[], IEnumerable<IconKeyMapping>, IconMatchResult?>? matcher = null)
{
    private readonly Func<double[], IEnumerable<IconKeyMapping>, IconMatchResult?> _matcher =
        matcher ?? IconKeyMappingMatcher.Match;
    private readonly Dictionary<Guid, (byte[] Source, PixelIconCandidate Candidate)> _pixelCandidates = [];

    public IconMatchResult? Match(double[] signature, IEnumerable<IconKeyMapping> mappings)
        => _matcher(signature, mappings);

    public PixelIconMatch? Match(PixelIconTemplate template, IEnumerable<IconKeyMapping> mappings)
    {
        List<PixelIconCandidate> candidates = [];
        foreach (var mapping in mappings.Where(mapping => mapping.Enabled))
        {
            if (_pixelCandidates.TryGetValue(mapping.Id, out var cached)
                && ReferenceEquals(cached.Source, mapping.PixelTemplateData))
            {
                candidates.Add(cached.Candidate);
                continue;
            }
            var decoded = PixelIconTemplate.Deserialize(mapping.PixelTemplateData);
            if (decoded is null) continue;
            var candidate = new PixelIconCandidate(mapping, decoded);
            _pixelCandidates[mapping.Id] = (mapping.PixelTemplateData, candidate);
            candidates.Add(candidate);
        }
        return PixelIconTemplateMatcher.Match(template, candidates);
    }

    public void Reset() => _pixelCandidates.Clear();
}
