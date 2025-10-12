using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Color = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;

namespace Edi.ImageWatermark;

public interface IImageWatermarker
{
    MemoryStream AddWatermark(string watermarkText, Color color,
        WatermarkPosition watermarkPosition = WatermarkPosition.BottomRight,
        int textPadding = 10,
        int fontSize = 20,
        Font font = null);
}

public sealed class ImageWatermarker : IDisposable, IImageWatermarker
{
    private readonly bool _skipImageSize;
    private readonly int _pixelsThreshold;
    private readonly Stream _originImageStream;
    private readonly string _imgExtensionName;
    private bool _disposed;

    public ImageWatermarker(Stream originImageStream, string imgExtensionName, int pixelsThreshold = 0)
    {
        _originImageStream = originImageStream ?? throw new ArgumentNullException(nameof(originImageStream));
        _imgExtensionName = imgExtensionName ?? throw new ArgumentNullException(nameof(imgExtensionName));

        if (pixelsThreshold > 0)
        {
            _skipImageSize = true;
            _pixelsThreshold = pixelsThreshold;
        }
    }

    /// <summary>
    /// Adds a text watermark to an image.
    /// </summary>
    /// <param name="watermarkText">The text to display as watermark.</param>
    /// <param name="color">The color of the watermark text.</param>
    /// <param name="watermarkPosition">The position where the watermark should be placed.</param>
    /// <param name="textPadding">The padding around the watermark text in pixels.</param>
    /// <param name="fontSize">The font size of the watermark text.</param>
    /// <param name="font">Optional custom font. If null, a default font will be used.</param>
    /// <returns>A MemoryStream containing the watermarked image, or null if the image doesn't meet the pixel threshold.</returns>
    /// <exception cref="ArgumentException">Thrown when watermarkText is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when textPadding is negative or fontSize is not positive.</exception>
    public MemoryStream AddWatermark(string watermarkText, Color color,
        WatermarkPosition watermarkPosition = WatermarkPosition.BottomRight,
        int textPadding = 10,
        int fontSize = 20,
        Font font = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(watermarkText))
            throw new ArgumentException("Watermark text cannot be null or whitespace.", nameof(watermarkText));

        if (textPadding < 0)
            throw new ArgumentOutOfRangeException(nameof(textPadding), "Text padding cannot be negative.");

        if (fontSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(fontSize), "Font size must be positive.");

        // Reset stream position before reading
        if (_originImageStream.CanSeek)
        {
            _originImageStream.Position = 0;
        }

        using var img = Image.Load(_originImageStream);

        if (_skipImageSize && img.Height * img.Width < _pixelsThreshold)
        {
            return null;
        }

        var watermarkedStream = new MemoryStream();

        try
        {
            var f = font ?? GetDefaultFont(fontSize);
            var textSize = TextMeasurer.MeasureBounds(watermarkText, new TextOptions(f));
            var (x, y) = GetWatermarkPosition(watermarkPosition, img.Width, img.Height, textSize.Width, textSize.Height, textPadding);

            img.Mutate(ctx => ctx.DrawText(watermarkText, f, color, new PointF(x, y)));

            SaveImage(img, watermarkedStream);
            watermarkedStream.Position = 0;

            return watermarkedStream;
        }
        catch
        {
            watermarkedStream?.Dispose();
            throw;
        }
    }

    private static Font GetDefaultFont(int fontSize)
    {
        var fontName = GetFontName();
        return SystemFonts.CreateFont(fontName, fontSize, FontStyle.Bold);
    }

    private static string GetFontName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "Arial";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetAvailableFontForLinux();
        }

        throw new PlatformNotSupportedException($"Unsupported platform: {RuntimeInformation.OSDescription}");
    }

    private static (int x, int y) GetWatermarkPosition(WatermarkPosition position, int imgWidth, int imgHeight, float textWidth, float textHeight, int padding)
    {
        // Ensure watermark fits within image bounds
        var maxX = Math.Max(0, imgWidth - (int)Math.Ceiling(textWidth));
        var maxY = Math.Max(0, imgHeight - (int)Math.Ceiling(textHeight));

        return position switch
        {
            WatermarkPosition.TopLeft => (Math.Min(padding, maxX), Math.Min(padding, maxY)),
            WatermarkPosition.TopRight => (Math.Max(padding, imgWidth - (int)Math.Ceiling(textWidth) - padding), Math.Min(padding, maxY)),
            WatermarkPosition.BottomLeft => (Math.Min(padding, maxX), Math.Max(padding, imgHeight - (int)Math.Ceiling(textHeight) - padding)),
            WatermarkPosition.BottomRight => (Math.Max(padding, imgWidth - (int)Math.Ceiling(textWidth) - padding), Math.Max(padding, imgHeight - (int)Math.Ceiling(textHeight) - padding)),
            WatermarkPosition.Center => (Math.Max(0, (imgWidth - (int)Math.Ceiling(textWidth)) / 2), Math.Max(0, (imgHeight - (int)Math.Ceiling(textHeight)) / 2)),
            _ => throw new ArgumentOutOfRangeException(nameof(position), position, "Invalid watermark position")
        };
    }

    private void SaveImage(Image img, MemoryStream stream)
    {
        try
        {
            var extension = _imgExtensionName.ToLowerInvariant();
            switch (extension)
            {
                case ".png":
                    img.SaveAsPng(stream);
                    break;
                case ".jpg":
                case ".jpeg":
                    img.SaveAsJpeg(stream);
                    break;
                case ".bmp":
                    img.SaveAsBmp(stream);
                    break;
                case ".gif":
                    img.SaveAsGif(stream);
                    break;
                case ".webp":
                    img.SaveAsWebp(stream);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported image format: {_imgExtensionName}");
            }
        }
        catch (Exception ex) when (ex is not NotSupportedException)
        {
            throw new InvalidOperationException($"Failed to save image as {_imgExtensionName}", ex);
        }
    }

    private static string GetAvailableFontForLinux()
    {
        var fontList = new[]
        {
            "Arial",
            "Verdana",
            "Helvetica",
            "Tahoma",
            "Open Sans",
            "DejaVu Sans",
            "DejaVu Sans Mono",
            "Ubuntu Mono",
            "Liberation Sans",
            "Monospace"
        };

        var availableFont = fontList.FirstOrDefault(fontName => SystemFonts.Collection.TryGet(fontName, out _));
        
        if (availableFont == null)
        {
            throw new InvalidOperationException(
                "No suitable font found on this Linux system. Available fonts: " +
                string.Join(", ", SystemFonts.Collection.Families.Select(f => f.Name)));
        }

        return availableFont;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _originImageStream?.Dispose();
            _disposed = true;
        }
    }
}