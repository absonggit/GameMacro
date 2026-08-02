using GameMacro.App.ViewModels;

namespace GameMacro.App.Detection;

public static class SingleIconMappingBuilder
{
    private const double DuplicateDistance = .06;

    public static PendingIconMapping Build(CapturedRegion captured)
        => new()
        {
            PreviewPng = captured.PreviewPng,
            Signature = captured.Signature.ToArray(),
            MatchThreshold = .18,
            PixelTemplateData = PixelIconTemplateBuilder
                .Create(captured.Pixels, captured.Width, captured.Height)
                .Serialize()
        };

    public static bool IsDuplicate(
        PendingIconMapping candidate,
        IEnumerable<PendingIconMapping> existing)
        => existing.Any(item => item.Signature.Length == candidate.Signature.Length
            && IconVisualSignature.Distance(item.Signature, candidate.Signature) <= DuplicateDistance);
}
