namespace GameMacro.App.Detection;

public sealed record CapturedSkillImage(double[] Signature, string PreviewPng);

public sealed record CapturedRegion(byte[] Pixels, int Width, int Height, double[] Signature, string PreviewPng);
