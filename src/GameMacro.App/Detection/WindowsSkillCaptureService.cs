using System.Runtime.InteropServices;
using GameMacro.App.Platform;
using GameMacro.Core.Models;

namespace GameMacro.App.Detection;

public sealed class WindowsSkillCaptureService(WindowsWindowService windows)
{
    public IReadOnlyDictionary<Guid, double[]> CaptureSignatures(MacroProfile profile, IEnumerable<MacroRule> rules)
    {
        var selected = rules.Where(rule => rule.HasVisualCalibration).ToList();
        if (selected.Count == 0) return new Dictionary<Guid, double[]>();
        var handle = windows.FindWindow(profile);
        if (handle == 0 || NativeMethods.IsIconic(handle)) throw new InvalidOperationException("目标游戏窗口不可用。");
        if (!NativeMethods.GetClientRect(handle, out var client)) throw new InvalidOperationException("无法读取游戏客户区。");
        var clientWidth = client.Right - client.Left;
        var clientHeight = client.Bottom - client.Top;
        var regions = selected.Select(rule => (Rule: rule, Region: new NormalizedRegion(
            rule.DetectionX, rule.DetectionY, rule.DetectionWidth, rule.DetectionHeight)
            .ToPixels(clientWidth, clientHeight))).ToList();
        var left = regions.Min(item => item.Region.X);
        var top = regions.Min(item => item.Region.Y);
        var right = regions.Max(item => item.Region.X + item.Region.Width);
        var bottom = regions.Max(item => item.Region.Y + item.Region.Height);
        var origin = new NativeMethods.Point();
        if (!NativeMethods.ClientToScreen(handle, ref origin)) throw new InvalidOperationException("无法定位游戏客户区。");
        var frameWidth = right - left;
        var frameHeight = bottom - top;
        var frame = CaptureScreen(origin.X + left, origin.Y + top, frameWidth, frameHeight);
        Dictionary<Guid, double[]> result = [];
        foreach (var item in regions)
        {
            var pixels = BgraFrameCropper.Crop(frame, frameWidth, frameHeight,
                item.Region.X - left, item.Region.Y - top, item.Region.Width, item.Region.Height);
            result[item.Rule.Id] = IconStateClassifier.CreateSignature(pixels, item.Region.Width, item.Region.Height);
        }
        return result;
    }

    public double[] CaptureSignature(MacroProfile profile, MacroRule rule)
    {
        var frame = CapturePixels(profile, rule);
        return IconStateClassifier.CreateSignature(frame.Pixels, frame.Width, frame.Height);
    }

    public CapturedSkillImage Capture(MacroProfile profile, MacroRule rule)
    {
        var frame = CapturePixels(profile, rule);
        return new(
            IconStateClassifier.CreateSignature(frame.Pixels, frame.Width, frame.Height),
            PngPreviewCodec.EncodeBgra(frame.Pixels, frame.Width, frame.Height));
    }

    public double[] CaptureRegionSignature(MacroProfile profile)
    {
        var frame = CapturePixels(profile, new NormalizedRegion(profile.DetectionX, profile.DetectionY,
            profile.DetectionWidth, profile.DetectionHeight));
        return IconTemplateNormalizer.CreateSignature(frame.Pixels, frame.Width, frame.Height);
    }

    public PixelIconTemplate CaptureRegionTemplate(MacroProfile profile)
    {
        var frame = CapturePixels(profile, new NormalizedRegion(profile.DetectionX, profile.DetectionY,
            profile.DetectionWidth, profile.DetectionHeight));
        return PixelIconTemplateBuilder.Create(frame.Pixels, frame.Width, frame.Height);
    }

    public CapturedSkillImage CaptureRegion(MacroProfile profile)
    {
        var frame = CaptureRegion(profile, new NormalizedRegion(profile.DetectionX, profile.DetectionY,
            profile.DetectionWidth, profile.DetectionHeight));
        return new(frame.Signature, frame.PreviewPng);
    }

    public CapturedRegion CaptureRegion(MacroProfile profile, NormalizedRegion region)
    {
        var frame = CapturePixels(profile, region);
        return new(
            frame.Pixels,
            frame.Width,
            frame.Height,
            IconTemplateNormalizer.CreateSignature(frame.Pixels, frame.Width, frame.Height),
            PngPreviewCodec.EncodeBgra(frame.Pixels, frame.Width, frame.Height));
    }

    private (byte[] Pixels, int Width, int Height) CapturePixels(MacroProfile profile, MacroRule rule)
        => CapturePixels(profile, new NormalizedRegion(rule.DetectionX, rule.DetectionY,
            rule.DetectionWidth, rule.DetectionHeight));

    private (byte[] Pixels, int Width, int Height) CapturePixels(MacroProfile profile, NormalizedRegion normalizedRegion)
    {
        var handle = windows.FindWindow(profile);
        if (handle == 0 || NativeMethods.IsIconic(handle)) throw new InvalidOperationException("目标游戏窗口不可用。");
        if (!NativeMethods.GetClientRect(handle, out var client)) throw new InvalidOperationException("无法读取游戏客户区。");
        var clientWidth = client.Right - client.Left;
        var clientHeight = client.Bottom - client.Top;
        var region = normalizedRegion.ToPixels(clientWidth, clientHeight);
        var origin = new NativeMethods.Point();
        if (!NativeMethods.ClientToScreen(handle, ref origin)) throw new InvalidOperationException("无法定位游戏客户区。");
        var pixels = CaptureScreen(origin.X + region.X, origin.Y + region.Y, region.Width, region.Height);
        return (pixels, region.Width, region.Height);
    }

    public (int X, int Y, int Width, int Height) GetClientScreenBounds(MacroProfile profile)
    {
        var handle = windows.FindWindow(profile);
        if (handle == 0 || !NativeMethods.GetClientRect(handle, out var client)) throw new InvalidOperationException("目标游戏窗口不可用。");
        var origin = new NativeMethods.Point();
        if (!NativeMethods.ClientToScreen(handle, ref origin)) throw new InvalidOperationException("无法定位游戏客户区。");
        return (origin.X, origin.Y, client.Right - client.Left, client.Bottom - client.Top);
    }

    private static byte[] CaptureScreen(int x, int y, int width, int height)
    {
        var screenDc = NativeMethods.GetDC(0);
        var memoryDc = NativeMethods.CreateCompatibleDC(screenDc);
        nint bitmap = 0;
        nint previous = 0;
        try
        {
            var info = new NativeMethods.BitmapInfo
            {
                Header = new NativeMethods.BitmapInfoHeader
                {
                    Size = (uint)Marshal.SizeOf<NativeMethods.BitmapInfoHeader>(), Width = width, Height = -height,
                    Planes = 1, BitCount = 32, Compression = 0, SizeImage = (uint)(width * height * 4)
                }
            };
            bitmap = NativeMethods.CreateDIBSection(memoryDc, ref info, NativeMethods.DibRgbColors, out var bits, 0, 0);
            if (bitmap == 0 || bits == 0) throw new InvalidOperationException("无法创建截图缓冲区。");
            previous = NativeMethods.SelectObject(memoryDc, bitmap);
            if (!NativeMethods.BitBlt(memoryDc, 0, 0, width, height, screenDc, x, y, NativeMethods.SrcCopy))
                throw new InvalidOperationException("游戏画面截图失败。");
            var result = new byte[width * height * 4];
            Marshal.Copy(bits, result, 0, result.Length);
            return result;
        }
        finally
        {
            if (previous != 0) NativeMethods.SelectObject(memoryDc, previous);
            if (bitmap != 0) NativeMethods.DeleteObject(bitmap);
            if (memoryDc != 0) NativeMethods.DeleteDC(memoryDc);
            if (screenDc != 0) NativeMethods.ReleaseDC(0, screenDc);
        }
    }
}
