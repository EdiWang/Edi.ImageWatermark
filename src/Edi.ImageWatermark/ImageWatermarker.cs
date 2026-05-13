using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Edi.ImageWatermark;

public interface IImageWatermarker
{
    MemoryStream AddWatermark(string watermarkText, SKColor color,
        WatermarkPosition watermarkPosition = WatermarkPosition.BottomRight,
        int textPadding = 10,
        int fontSize = 20,
        SKTypeface typeface = null);
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
    /// <param name="typeface">Optional custom typeface. If null, a default typeface will be used.</param>
    /// <returns>A MemoryStream containing the watermarked image, or null if the image doesn't meet the pixel threshold.</returns>
    /// <exception cref="ArgumentException">Thrown when watermarkText is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when textPadding is negative or fontSize is not positive.</exception>
    public MemoryStream AddWatermark(string watermarkText, SKColor color,
        WatermarkPosition watermarkPosition = WatermarkPosition.BottomRight,
        int textPadding = 10,
        int fontSize = 20,
        SKTypeface typeface = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(watermarkText))
            throw new ArgumentException("Watermark text cannot be null or whitespace.", nameof(watermarkText));

        if (textPadding < 0)
            throw new ArgumentOutOfRangeException(nameof(textPadding), "Text padding cannot be negative.");

        if (fontSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(fontSize), "Font size must be positive.");

        var imageBytes = ReadOriginImageBytes();
        var detectedFormat = DetectFormat(imageBytes);
        using var img = SKBitmap.Decode(imageBytes) ?? throw new InvalidOperationException("Unable to decode image.");

        if (_checkPixelThreshold && img.Height * img.Width < _pixelsThreshold)
        {
            return null;
        }

        var watermarkedStream = new MemoryStream();

        try
        {
            using var canvas = new SKCanvas(img);
            using var paint = new SKPaint
            {
                Color = color,
                IsAntialias = true
            };
            var effectiveTypeface = typeface;
            using var ownedTypeface = typeface is null ? GetDefaultTypeface() : null;
            effectiveTypeface ??= ownedTypeface;
            using var font = new SKFont(effectiveTypeface, fontSize);

            font.MeasureText(watermarkText, out var textBounds, paint);
            var (x, y) = GetWatermarkPosition(watermarkPosition, img.Width, img.Height, textBounds.Width, textBounds.Height, textPadding);

            canvas.DrawText(watermarkText, x - textBounds.Left, y - textBounds.Top, font, paint);
            canvas.Flush();

            using var image = SKImage.FromBitmap(img);
            using var data = image.Encode(detectedFormat, 100);
            data.SaveTo(watermarkedStream);
            watermarkedStream.Position = 0;

            return watermarkedStream;
        }
        catch
        {
            watermarkedStream?.Dispose();
            throw;
        }
    }

    private byte[] ReadOriginImageBytes()
    {
        if (_originImageStream.CanSeek)
        {
            _originImageStream.Position = 0;
        }

        using var buffer = new MemoryStream();
        _originImageStream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static SKEncodedImageFormat DetectFormat(byte[] imageBytes)
    {
        using var stream = new SKMemoryStream(imageBytes);
        using var codec = SKCodec.Create(stream) ?? throw new InvalidOperationException("Unable to detect image format.");

        return codec.EncodedFormat switch
        {
            SKEncodedImageFormat.Bmp => SKEncodedImageFormat.Bmp,
            SKEncodedImageFormat.Gif => SKEncodedImageFormat.Gif,
            SKEncodedImageFormat.Jpeg => SKEncodedImageFormat.Jpeg,
            SKEncodedImageFormat.Png => SKEncodedImageFormat.Png,
            SKEncodedImageFormat.Webp => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Png
        };
    }

    private static SKEncodedImageFormat GetSupportedOutputFormat(SKEncodedImageFormat format)
    {
        return format switch
        {
            SKEncodedImageFormat.Jpeg => SKEncodedImageFormat.Jpeg,
            SKEncodedImageFormat.Png => SKEncodedImageFormat.Png,
            SKEncodedImageFormat.Webp => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Png
        };
    }

    private SKTypeface GetDefaultTypeface()
    {
        if (!string.IsNullOrEmpty(_customFontPath))
        {
            if (!File.Exists(_customFontPath))
                throw new FileNotFoundException($"Custom font file not found: {_customFontPath}", _customFontPath);
            return LoadTypefaceFromFile(_customFontPath);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return GetLinuxTypeface();

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

    private static SKTypeface GetLinuxTypeface()
    {
        string[] preferredFonts =
        [
            "Arial", "Liberation Sans", "DejaVu Sans", "Open Sans",
            "Verdana", "Tahoma", "Ubuntu", "DejaVu Sans Mono", "Ubuntu Mono", "Monospace"
        ];

        foreach (var name in preferredFonts)
        {
            var typeface = SKTypeface.FromFamilyName(name, SKFontStyle.Bold);
            if (typeface is not null && !string.Equals(typeface.FamilyName, SKTypeface.Default.FamilyName, StringComparison.OrdinalIgnoreCase))
                return typeface;
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
                return LoadTypefaceFromFile(fontFile);
        }

        throw new InvalidOperationException(
            "No suitable font found on this Linux system. " +
            "Install fonts via your package manager (e.g., 'apt-get install -y fonts-liberation' on Debian/Ubuntu, " +
            "or 'apk add ttf-liberation' on Alpine Linux), " +
            "or pass a font file path to the ImageWatermarker constructor.");
    }

    private static SKTypeface LoadTypefaceFromFile(string fontFilePath)
    {
        return SKTypeface.FromFile(fontFilePath) ?? throw new InvalidOperationException($"Unable to load font file: {fontFilePath}");
    }

    public void Dispose()
    {
        _disposed = true;
    }
}