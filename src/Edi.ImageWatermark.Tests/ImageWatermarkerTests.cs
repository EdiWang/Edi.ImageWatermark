using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Edi.ImageWatermark.Tests;

[TestClass]
public class ImageWatermarkerTests
{
    [TestMethod]
    public void AddWatermark_ShouldAddWatermarkToImage()
    {
        var imageStream = new MemoryStream();
        using var image = new Image<Rgba32>(100, 100);
        image.Save(imageStream, new PngEncoder());

        imageStream.Position = 0;

        using var watermarker = new ImageWatermarker(imageStream, ".png");
        var watermarkText = "Watermark";
        var color = Color.Red;
        var watermarkPosition = WatermarkPosition.BottomRight;
        var textPadding = 10;
        var fontSize = 20;
        var font = null as Font;

        var result = watermarker.AddWatermark(watermarkText, color, watermarkPosition, textPadding, fontSize, font);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }
}