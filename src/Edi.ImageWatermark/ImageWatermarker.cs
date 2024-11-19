using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
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

public class ImageWatermarker : IDisposable, IImageWatermarker
{
    private readonly bool _skipImageSize;
    private readonly int _pixelsThreshold;
    private readonly Stream _originImageStream;
    private readonly string _imgExtensionName;

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

    public MemoryStream AddWatermark(string watermarkText, Color color,
        WatermarkPosition watermarkPosition = WatermarkPosition.BottomRight,
        int textPadding = 10,
        int fontSize = 20,
        Font font = null)
    {
        if (string.IsNullOrEmpty(watermarkText)) throw new ArgumentNullException(nameof(watermarkText));

        using var img = Image.Load(_originImageStream);
        if (_skipImageSize && img.Height * img.Width < _pixelsThreshold)
        {
            return null;
        }

        var watermarkedStream = new MemoryStream();

        string fontName = GetFontName();
        var f = font ?? SystemFonts.CreateFont(fontName, fontSize, FontStyle.Bold);
        var textSize = TextMeasurer.MeasureBounds(watermarkText, new TextOptions(f));
        var (x, y) = GetWatermarkPosition(watermarkPosition, img.Width, img.Height, textSize.Width, textSize.Height, textPadding);

        img.Mutate(ctx => ctx.DrawText(watermarkText, f, color, new PointF(x, y)));

        SaveImage(img, watermarkedStream);

        return watermarkedStream;
    }

    private string GetFontName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "Arial";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetAvailableFontForLinux();
        }

        throw new PlatformNotSupportedException("Unsupported platform");
    }

    private static (int x, int y) GetWatermarkPosition(WatermarkPosition position, int imgWidth, int imgHeight, float textWidth, float textHeight, int padding)
    {
        return position switch
        {
            WatermarkPosition.TopLeft => (padding, padding),
            WatermarkPosition.TopRight => (imgWidth - (int)textWidth - padding, padding),
            WatermarkPosition.BottomLeft => (padding, imgHeight - (int)textHeight - padding),
            WatermarkPosition.BottomRight => (imgWidth - (int)textWidth - padding, imgHeight - (int)textHeight - padding),
            WatermarkPosition.Center => ((imgWidth - (int)textWidth) / 2, (imgHeight - (int)textHeight) / 2),
            _ => (padding, padding)
        };
    }

    private void SaveImage(Image img, MemoryStream stream)
    {
        try
        {
            switch (_imgExtensionName.ToLower())
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
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to save image", ex);
        }
    }

    public void Dispose()
    {
        _originImageStream?.Dispose();
    }

    private static string GetAvailableFontForLinux()
    {
        var fontList = new[]
        {
            "Arial",
            "Verdana",
            "Helvetica",
            "Tahoma",
            "Terminal",
            "Open Sans",
            "Monospace",
            "Ubuntu Mono",
            "DejaVu Sans",
            "DejaVu Sans Mono"
        };
        return fontList.FirstOrDefault(fontName => SystemFonts.Collection.TryGet(fontName, out _)) ?? "Arial";
    }
}