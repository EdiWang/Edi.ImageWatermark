using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Edi.ImageWatermark
{
    public interface IImageWatermarker
    {
        IImageWatermarker SkipImageSize(int pixelsThreshold);

        MemoryStream AddWatermark(string watermarkText, Color color,
            WatermarkPosition watermarkPosition = WatermarkPosition.BottomRight,
            int textPadding = 10,
            int fontSize = 20,
            Font font = null,
            bool textAntiAlias = true);
    }

    public class ImageWatermarker : IDisposable, IImageWatermarker
    {
        private bool _skipImageSize;
        private int _pixelsThreshold;
        private readonly Stream _originImageStream;
        private readonly string _imgExtensionName;

        public ImageWatermarker(Stream originImageStream, string imgExtensionName)
        {
            _originImageStream = originImageStream;
            _imgExtensionName = imgExtensionName;
        }

        public IImageWatermarker SkipImageSize(int pixelsThreshold)
        {
            if (pixelsThreshold <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pixelsThreshold), "value must be greater than zero.");
            }

            _skipImageSize = true;
            _pixelsThreshold = pixelsThreshold;

            return this;
        }

        public MemoryStream AddWatermark(string watermarkText, Color color,
            WatermarkPosition watermarkPosition = WatermarkPosition.BottomRight,
            int textPadding = 10,
            int fontSize = 20,
            Font font = null,
            bool textAntiAlias = true)
        {
            using var watermarkedStream = new MemoryStream();
            using var img = Image.FromStream(_originImageStream);
            if (_skipImageSize && img.Height * img.Width < _pixelsThreshold)
            {
                return null;
            }

            using var graphic = Graphics.FromImage(img);
            if (textAntiAlias)
            {
                graphic.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            }

            var brush = new SolidBrush(color);

            var f = font ?? new Font(FontFamily.GenericSansSerif, fontSize,
                FontStyle.Bold, GraphicsUnit.Pixel);

            var textSize = graphic.MeasureString(watermarkText, f);
            int x = textPadding, y = textPadding;

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

            graphic.DrawString(watermarkText, f, brush, new Point(x, y));

            ImageFormat fmt = null;
            switch (_imgExtensionName)
            {
                case ".png":
                    fmt = ImageFormat.Png;
                    break;
                case ".jpg":
                case ".jpeg":
                    fmt = ImageFormat.Jpeg;
                    break;
                case ".bmp":
                    fmt = ImageFormat.Bmp;
                    break;
            }
            img.Save(watermarkedStream, fmt);
            return watermarkedStream;
        }

        public void Dispose()
        {
            _originImageStream?.Dispose();
        }
    }
}
