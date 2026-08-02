using GameMacro.App.Detection;

namespace GameMacro.App.Tests.Detection;

public sealed class BgraFrameCropperTests
{
    [Fact]
    public void Crops_rectangular_region_from_single_bgra_frame()
    {
        var pixels = Enumerable.Range(0, 24).Select(value => (byte)value).ToArray();

        var cropped = BgraFrameCropper.Crop(pixels, 3, 2, 1, 0, 2, 2);

        Assert.Equal(new byte[] { 4, 5, 6, 7, 8, 9, 10, 11, 16, 17, 18, 19, 20, 21, 22, 23 }, cropped);
    }
}
