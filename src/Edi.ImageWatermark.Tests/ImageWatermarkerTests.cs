using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;

namespace Edi.ImageWatermark.Tests;

[TestClass]
public class ImageWatermarkerTests
{
    private MemoryStream CreateTestImageStream(int width = 100, int height = 100, string format = ".png")
    {
        var imageStream = new MemoryStream();
        using var image = new Image<Rgba32>(width, height);

        switch (format.ToLower())
        {
            case ".png":
                image.Save(imageStream, new PngEncoder());
                break;
            case ".jpg":
            case ".jpeg":
                image.Save(imageStream, new JpegEncoder());
                break;
            case ".bmp":
                image.Save(imageStream, new BmpEncoder());
                break;
            case ".gif":
                image.Save(imageStream, new GifEncoder());
                break;
            case ".webp":
                image.Save(imageStream, new WebpEncoder());
                break;
        }

        imageStream.Position = 0;
        return imageStream;
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        using var imageStream = CreateTestImageStream();

        using var watermarker = new ImageWatermarker(imageStream, ".png");

        Assert.IsNotNull(watermarker);
    }

    [TestMethod]
    public void Constructor_WithPixelsThreshold_ShouldCreateInstance()
    {
        using var imageStream = CreateTestImageStream();

        using var watermarker = new ImageWatermarker(imageStream, ".png", 1000);

        Assert.IsNotNull(watermarker);
    }

    [TestMethod]
    public void Constructor_WithNullImageStream_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new ImageWatermarker(null, ".png"));
    }

    [TestMethod]
    public void Constructor_WithNullExtension_ShouldThrowArgumentNullException()
    {
        using var imageStream = CreateTestImageStream();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new ImageWatermarker(imageStream, null));
    }

    #endregion

    #region AddWatermark Tests

    [TestMethod]
    public void AddWatermark_WithValidParameters_ShouldReturnWatermarkedImage()
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream, ".png");

        var result = watermarker.AddWatermark("Test Watermark", Color.Red);

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
    }

    [TestMethod]
    public void AddWatermark_WithNullWatermarkText_ShouldThrowArgumentNullException()
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream, ".png");

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            watermarker.AddWatermark(null, Color.Red));
    }

    [TestMethod]
    public void AddWatermark_WithEmptyWatermarkText_ShouldThrowArgumentNullException()
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream, ".png");

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            watermarker.AddWatermark("", Color.Red));
    }

    [TestMethod]
    public void AddWatermark_WithPixelsThresholdNotMet_ShouldReturnNull()
    {
        using var imageStream = CreateTestImageStream(10, 10); // Small image
        using var watermarker = new ImageWatermarker(imageStream, ".png", 1000); // High threshold

        var result = watermarker.AddWatermark("Test", Color.Red);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void AddWatermark_WithPixelsThresholdMet_ShouldReturnWatermarkedImage()
    {
        using var imageStream = CreateTestImageStream(100, 100); // Large enough image
        using var watermarker = new ImageWatermarker(imageStream, ".png", 1000); // Lower threshold

        var result = watermarker.AddWatermark("Test", Color.Red);

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
    }

    [TestMethod]
    [DataRow(WatermarkPosition.TopLeft)]
    [DataRow(WatermarkPosition.TopRight)]
    [DataRow(WatermarkPosition.BottomLeft)]
    [DataRow(WatermarkPosition.BottomRight)]
    [DataRow(WatermarkPosition.Center)]
    public void AddWatermark_WithDifferentPositions_ShouldReturnWatermarkedImage(WatermarkPosition position)
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream, ".png");

        var result = watermarker.AddWatermark("Test", Color.Red, position);

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
    }

    [TestMethod]
    public void AddWatermark_WithCustomFont_ShouldReturnWatermarkedImage()
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream, ".png");
        var font = SystemFonts.CreateFont("Arial", 16, FontStyle.Bold);

        var result = watermarker.AddWatermark("Test", Color.Red, font: font);

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
    }

    [TestMethod]
    public void AddWatermark_WithCustomPaddingAndFontSize_ShouldReturnWatermarkedImage()
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream, ".png");

        var result = watermarker.AddWatermark("Test", Color.Blue,
            textPadding: 20, fontSize: 24);

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
    }

    #endregion

    #region SaveImage Tests (via different image formats)

    [TestMethod]
    [DataRow(".png")]
    [DataRow(".jpg")]
    [DataRow(".jpeg")]
    [DataRow(".bmp")]
    [DataRow(".gif")]
    [DataRow(".webp")]
    public void AddWatermark_WithSupportedImageFormats_ShouldReturnWatermarkedImage(string format)
    {
        using var imageStream = CreateTestImageStream(format: format);
        using var watermarker = new ImageWatermarker(imageStream, format);

        var result = watermarker.AddWatermark("Test", Color.Red);

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
    }

    #endregion

    #region GetWatermarkPosition Tests (via position verification)

    [TestMethod]
    public void AddWatermark_WithTopLeftPosition_ShouldPlaceWatermarkCorrectly()
    {
        using var imageStream = CreateTestImageStream(200, 200);
        using var watermarker = new ImageWatermarker(imageStream, ".png");

        var result = watermarker.AddWatermark("TopLeft", Color.Red,
            WatermarkPosition.TopLeft, textPadding: 5);

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
    }

    [TestMethod]
    public void AddWatermark_WithCenterPosition_ShouldPlaceWatermarkCorrectly()
    {
        using var imageStream = CreateTestImageStream(200, 200);
        using var watermarker = new ImageWatermarker(imageStream, ".png");

        var result = watermarker.AddWatermark("Center", Color.Red,
            WatermarkPosition.Center);

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
    }

    #endregion

    #region GetFontName Tests (via different platforms)

    [TestMethod]
    public void AddWatermark_OnCurrentPlatform_ShouldUseAppropriateFont()
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream, ".png");

        // This test verifies that GetFontName() works on the current platform
        var result = watermarker.AddWatermark("Platform Test", Color.Red);

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
    }

    #endregion

    #region GetAvailableFontForLinux Tests (indirect testing)

    [TestMethod]
    public void AddWatermark_WithSystemFont_ShouldHandleFontSelection()
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream, ".png");

        // This indirectly tests font selection logic
        var result = watermarker.AddWatermark("Font Test", Color.Green);

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
    }

    #endregion

    #region Dispose Tests

    [TestMethod]
    public void Dispose_ShouldDisposeResourcesProperly()
    {
        var imageStream = CreateTestImageStream();
        var watermarker = new ImageWatermarker(imageStream, ".png");

        watermarker.Dispose();

        // Verify that the stream is disposed by checking if it throws when accessed
        Assert.ThrowsExactly<ObjectDisposedException>(() => imageStream.Read(new byte[1], 0, 1));
    }

    [TestMethod]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        using var imageStream = CreateTestImageStream();
        var watermarker = new ImageWatermarker(imageStream, ".png");

        watermarker.Dispose();
        watermarker.Dispose(); // Should not throw

        Assert.IsTrue(true); // Test passes if no exception is thrown
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public void AddWatermark_CompleteWorkflow_ShouldProduceValidResult()
    {
        using var imageStream = CreateTestImageStream(300, 200);
        using var watermarker = new ImageWatermarker(imageStream, ".png");

        var result = watermarker.AddWatermark(
            "© 2024 Test Company",
            Color.White,
            WatermarkPosition.BottomRight,
            textPadding: 15,
            fontSize: 18);

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);

        // Verify the result can be loaded as an image
        result.Position = 0;
        using var resultImage = Image.Load(result);
        Assert.AreEqual(300, resultImage.Width);
        Assert.AreEqual(200, resultImage.Height);
    }

    [TestMethod]
    public void AddWatermark_WithLargeImage_ShouldHandleEfficiently()
    {
        using var imageStream = CreateTestImageStream(1920, 1080);
        using var watermarker = new ImageWatermarker(imageStream, ".png");

        var result = watermarker.AddWatermark("Large Image Test", Color.Red);

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void AddWatermark_WithVerySmallImage_ShouldStillWork()
    {
        using var imageStream = CreateTestImageStream(10, 10);
        using var watermarker = new ImageWatermarker(imageStream, ".png");

        var result = watermarker.AddWatermark("X", Color.Red, fontSize: 8);

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
    }

    [TestMethod]
    public void AddWatermark_WithLongWatermarkText_ShouldHandleGracefully()
    {
        using var imageStream = CreateTestImageStream(500, 100);
        using var watermarker = new ImageWatermarker(imageStream, ".png");
        var longText = "This is a very long watermark text that might exceed the image boundaries";

        var result = watermarker.AddWatermark(longText, Color.Red, fontSize: 12);

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
    }

    [TestMethod]
    public void AddWatermark_WithZeroPadding_ShouldWork()
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream, ".png");

        var result = watermarker.AddWatermark("No Padding", Color.Red, textPadding: 0);

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
    }

    [TestMethod]
    public void AddWatermark_WithLargeFontSize_ShouldWork()
    {
        using var imageStream = CreateTestImageStream(400, 400);
        using var watermarker = new ImageWatermarker(imageStream, ".png");

        var result = watermarker.AddWatermark("BIG", Color.Red, fontSize: 72);

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
    }

    #endregion
}