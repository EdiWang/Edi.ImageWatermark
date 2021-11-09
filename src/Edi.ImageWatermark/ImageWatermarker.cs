using System;
using System.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;

namespace Edi.ImageWatermark
{
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
            _originImageStream = originImageStream;
            _imgExtensionName = imgExtensionName;

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
            using var img = Image.Load(_originImageStream);
            if (_skipImageSize && img.Height * img.Width < _pixelsThreshold)
            {
                return null;
            }

            using var watermarkedStream = new MemoryStream();

            var f = font ?? SystemFonts.CreateFont("Arial", fontSize, FontStyle.Bold);
            var textSize = TextMeasurer.Measure(watermarkText, new RendererOptions(f));
            int x, y;

            switch (watermarkPosition)
            {
                case WatermarkPosition.TopLeft:
                    x = textPadding; y = textPadding;
                    break;
                case WatermarkPosition.TopRight:
                    x = img.Width - (int)textSize.Width - textPadding;
                    y = textPadding;
                    break;
                case WatermarkPosition.BottomLeft:
                    x = textPadding;
                    y = img.Height - (int)textSize.Height - textPadding;
                    break;
                case WatermarkPosition.BottomRight:
                    x = img.Width - (int)textSize.Width - textPadding;
                    y = img.Height - (int)textSize.Height - textPadding;
                    break;
                default:
                    x = textPadding; y = textPadding;
                    break;
            }

            img.Mutate(ctx => ctx.DrawText(watermarkText, f, color, new PointF(x, y)));

            switch (_imgExtensionName)
            {
                case ".png":
                    img.SaveAsPng(watermarkedStream);
                    break;
                case ".jpg":
                case ".jpeg":
                    img.SaveAsJpeg(watermarkedStream);
                    break;
                case ".bmp":
                    img.SaveAsBmp(watermarkedStream);
                    break;
            }

            return watermarkedStream;
        }

        public void Dispose()
        {
            _originImageStream?.Dispose();
        }
    }
}
