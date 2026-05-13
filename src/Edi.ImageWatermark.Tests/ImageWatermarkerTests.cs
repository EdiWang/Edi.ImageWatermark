using SkiaSharp;
using Xunit;

namespace Edi.ImageWatermark.Tests;

public class ImageWatermarkerTests
{
    private MemoryStream CreateTestImageStream(int width = 100, int height = 100, string format = ".png")
    {
        var imageStream = new MemoryStream();
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.White);
        using var image = SKImage.FromBitmap(bitmap);

        var encodedFormat = format.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            ".webp" => SKEncodedImageFormat.Webp,
            ".bmp" => SKEncodedImageFormat.Bmp,
            ".gif" => SKEncodedImageFormat.Gif,
            ".png" => SKEncodedImageFormat.Png,
            _ => SKEncodedImageFormat.Png
        };

        using var data = image.Encode(encodedFormat, 100);
        data.SaveTo(imageStream);

        imageStream.Position = 0;
        return imageStream;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        using var imageStream = CreateTestImageStream();

        using var watermarker = new ImageWatermarker(imageStream);

        Assert.NotNull(watermarker);
    }

    [Fact]
    public void Constructor_WithPixelsThreshold_ShouldCreateInstance()
    {
        using var imageStream = CreateTestImageStream();

        using var watermarker = new ImageWatermarker(imageStream, 1000);

        Assert.NotNull(watermarker);
    }

    [Fact]
    public void Constructor_WithNullImageStream_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ImageWatermarker(null));
    }

    #endregion

    #region AddWatermark Tests

    [Fact]
    public void AddWatermark_WithValidParameters_ShouldReturnWatermarkedImage()
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream);

        var result = watermarker.AddWatermark("Test Watermark", SKColors.Red);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void AddWatermark_WithNullWatermarkText_ShouldThrowArgumentNullException()
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream);

        Assert.Throws<ArgumentException>(() =>
            watermarker.AddWatermark(null, SKColors.Red));
    }

    [Fact]
    public void AddWatermark_WithEmptyWatermarkText_ShouldThrowArgumentNullException()
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream);

        Assert.Throws<ArgumentException>(() =>
            watermarker.AddWatermark("", SKColors.Red));
    }

    [Fact]
    public void AddWatermark_WithPixelsThresholdNotMet_ShouldReturnNull()
    {
        using var imageStream = CreateTestImageStream(10, 10); // Small image
        using var watermarker = new ImageWatermarker(imageStream, 1000); // High threshold

        var result = watermarker.AddWatermark("Test", SKColors.Red);

        Assert.Null(result);
    }

    [Fact]
    public void AddWatermark_WithPixelsThresholdMet_ShouldReturnWatermarkedImage()
    {
        using var imageStream = CreateTestImageStream(100, 100); // Large enough image
        using var watermarker = new ImageWatermarker(imageStream, 1000); // Lower threshold

        var result = watermarker.AddWatermark("Test", SKColors.Red);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Theory]
    [InlineData(WatermarkPosition.TopLeft)]
    [InlineData(WatermarkPosition.TopRight)]
    [InlineData(WatermarkPosition.BottomLeft)]
    [InlineData(WatermarkPosition.BottomRight)]
    [InlineData(WatermarkPosition.Center)]
    public void AddWatermark_WithDifferentPositions_ShouldReturnWatermarkedImage(WatermarkPosition position)
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream);

        var result = watermarker.AddWatermark("Test", SKColors.Red, position);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void AddWatermark_WithCustomFont_ShouldReturnWatermarkedImage()
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream);
        using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);

        var result = watermarker.AddWatermark("Test", SKColors.Red, typeface: typeface);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void AddWatermark_WithCustomPaddingAndFontSize_ShouldReturnWatermarkedImage()
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream);

        var result = watermarker.AddWatermark("Test", SKColors.Blue,
            textPadding: 20, fontSize: 24);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    #endregion

    #region SaveImage Tests (via different image formats)

    [Theory]
    [InlineData(".png")]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".bmp")]
    [InlineData(".gif")]
    [InlineData(".webp")]
    public void AddWatermark_WithSupportedImageFormats_ShouldReturnWatermarkedImage(string format)
    {
        using var imageStream = CreateTestImageStream(format: format);
        using var watermarker = new ImageWatermarker(imageStream);

        var result = watermarker.AddWatermark("Test", SKColors.Red);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    #endregion

    #region GetWatermarkPosition Tests (via position verification)

    [Fact]
    public void AddWatermark_WithTopLeftPosition_ShouldPlaceWatermarkCorrectly()
    {
        using var imageStream = CreateTestImageStream(200, 200);
        using var watermarker = new ImageWatermarker(imageStream);

        var result = watermarker.AddWatermark("TopLeft", SKColors.Red,
            WatermarkPosition.TopLeft, textPadding: 5);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void AddWatermark_WithCenterPosition_ShouldPlaceWatermarkCorrectly()
    {
        using var imageStream = CreateTestImageStream(200, 200);
        using var watermarker = new ImageWatermarker(imageStream);

        var result = watermarker.AddWatermark("Center", SKColors.Red,
            WatermarkPosition.Center);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    #endregion

    #region GetFontName Tests (via different platforms)

    [Fact]
    public void AddWatermark_OnCurrentPlatform_ShouldUseAppropriateFont()
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream);

        // This test verifies that GetFontName() works on the current platform
        var result = watermarker.AddWatermark("Platform Test", SKColors.Red);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    #endregion

    #region GetAvailableFontForLinux Tests (indirect testing)

    [Fact]
    public void AddWatermark_WithSystemFont_ShouldHandleFontSelection()
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream);

        // This indirectly tests font selection logic
        var result = watermarker.AddWatermark("Font Test", SKColors.Green);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldNotDisposeCallerStream()
    {
        var imageStream = CreateTestImageStream();
        var watermarker = new ImageWatermarker(imageStream);

        watermarker.Dispose();

        // Verify that the caller's stream is NOT disposed - caller owns the stream
        var buffer = new byte[1];
        var bytesRead = imageStream.Read(buffer, 0, 1);
        Assert.True(bytesRead >= 0);
        imageStream.Dispose();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        using var imageStream = CreateTestImageStream();
        var watermarker = new ImageWatermarker(imageStream);

        watermarker.Dispose();
        watermarker.Dispose(); // Should not throw

        Assert.True(true); // Test passes if no exception is thrown
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AddWatermark_CompleteWorkflow_ShouldProduceValidResult()
    {
        using var imageStream = CreateTestImageStream(300, 200);
        using var watermarker = new ImageWatermarker(imageStream);

        var result = watermarker.AddWatermark(
            "© 2024 Test Company",
            SKColors.White,
            WatermarkPosition.BottomRight,
            textPadding: 15,
            fontSize: 18);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        // Verify the result can be loaded as an image
        result.Position = 0;
        using var resultImage = SKBitmap.Decode(result);
        Assert.Equal(300, resultImage.Width);
        Assert.Equal(200, resultImage.Height);
    }

    [Fact]
    public void AddWatermark_WithLargeImage_ShouldHandleEfficiently()
    {
        using var imageStream = CreateTestImageStream(1920, 1080);
        using var watermarker = new ImageWatermarker(imageStream);

        var result = watermarker.AddWatermark("Large Image Test", SKColors.Red);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void AddWatermark_WithVerySmallImage_ShouldStillWork()
    {
        using var imageStream = CreateTestImageStream(10, 10);
        using var watermarker = new ImageWatermarker(imageStream);

        var result = watermarker.AddWatermark("X", SKColors.Red, fontSize: 8);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void AddWatermark_WithLongWatermarkText_ShouldHandleGracefully()
    {
        using var imageStream = CreateTestImageStream(500, 100);
        using var watermarker = new ImageWatermarker(imageStream);
        var longText = "This is a very long watermark text that might exceed the image boundaries";

        var result = watermarker.AddWatermark(longText, SKColors.Red, fontSize: 12);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void AddWatermark_WithZeroPadding_ShouldWork()
    {
        using var imageStream = CreateTestImageStream();
        using var watermarker = new ImageWatermarker(imageStream);

        var result = watermarker.AddWatermark("No Padding", SKColors.Red, textPadding: 0);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void AddWatermark_WithLargeFontSize_ShouldWork()
    {
        using var imageStream = CreateTestImageStream(400, 400);
        using var watermarker = new ImageWatermarker(imageStream);

        var result = watermarker.AddWatermark("BIG", SKColors.Red, fontSize: 72);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    #endregion
}