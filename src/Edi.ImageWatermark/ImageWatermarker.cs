using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
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
    private readonly bool _checkPixelThreshold;
    private readonly int _pixelsThreshold;
    private readonly Stream _originImageStream;
    private readonly string _customFontPath;
    private bool _disposed;

    public ImageWatermarker(Stream originImageStream, int pixelsThreshold = 0, string customFontPath = null)
    {
        _originImageStream = originImageStream ?? throw new ArgumentNullException(nameof(originImageStream));
        _customFontPath = customFontPath;

        if (pixelsThreshold > 0)
        {
            _checkPixelThreshold = true;
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

        var detectedFormat = Image.DetectFormat(_originImageStream);

        if (_originImageStream.CanSeek)
        {
            _originImageStream.Position = 0;
        }

        using var img = Image.Load(_originImageStream);

        if (_checkPixelThreshold && img.Height * img.Width < _pixelsThreshold)
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

            img.Save(watermarkedStream, detectedFormat);
            watermarkedStream.Position = 0;

            return watermarkedStream;
        }
        catch
        {
            watermarkedStream?.Dispose();
            throw;
        }
    }

    private Font GetDefaultFont(int fontSize)
    {
        if (!string.IsNullOrEmpty(_customFontPath))
        {
            if (!File.Exists(_customFontPath))
                throw new FileNotFoundException($"Custom font file not found: {_customFontPath}", _customFontPath);
            return LoadFontFromFile(_customFontPath, fontSize);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return SystemFonts.CreateFont("Arial", fontSize, FontStyle.Bold);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return GetLinuxFont(fontSize);

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

    private static Font GetLinuxFont(int fontSize)
    {
        string[] preferredFonts =
        [
            "Arial", "Liberation Sans", "DejaVu Sans", "Open Sans",
            "Verdana", "Tahoma", "Ubuntu", "DejaVu Sans Mono", "Ubuntu Mono", "Monospace"
        ];

        foreach (var name in preferredFonts)
        {
            if (SystemFonts.Collection.TryGet(name, out var family))
                return family.CreateFont(fontSize, FontStyle.Bold);
        }

        // Try any registered system font
        foreach (var family in SystemFonts.Collection.Families)
        {
            return family.CreateFont(fontSize, FontStyle.Bold);
        }

        // Scan common font directories as a last resort (e.g., when fontconfig cache is unavailable)
        string[] fontDirs = ["/usr/share/fonts", "/usr/local/share/fonts"];
        foreach (var dir in fontDirs)
        {
            if (!Directory.Exists(dir)) continue;
            var fontFile = Directory.EnumerateFiles(dir, "*.ttf", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(dir, "*.otf", SearchOption.AllDirectories))
                .FirstOrDefault();
            if (fontFile is not null)
                return LoadFontFromFile(fontFile, fontSize);
        }

        throw new InvalidOperationException(
            "No suitable font found on this Linux system. " +
            "Install fonts via your package manager (e.g., 'apt-get install -y fonts-liberation' on Debian/Ubuntu, " +
            "or 'apk add ttf-liberation' on Alpine Linux), " +
            "or pass a font file path to the ImageWatermarker constructor.");
    }

    private static Font LoadFontFromFile(string fontFilePath, int fontSize)
    {
        var collection = new FontCollection();
        var family = collection.Add(fontFilePath);
        return family.CreateFont(fontSize, FontStyle.Bold);
    }

    public void Dispose()
    {
        _disposed = true;
    }
}