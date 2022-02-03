using System;
using System.Collections.Generic;
using System.Reflection;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace GeneratorCore.Helpers
{
    public static class ComposeHelper
    {
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        private static readonly FontCollection _fontCollection = new FontCollection();

        public static void Init()
        {
            var fonts = new[] {
                "Yu-Gi-Oh! Matrix Regular Small Caps 1.ttf",
                "Yu-Gi-Oh! Matrix Book.ttf",
            };
            foreach (var font in fonts)
            {
                using (var stream = _assembly.GetManifestResourceStream($"GeneratorCore.Resources.font.{font}"))
                    _fontCollection.Install(stream);
            }
        }

        public static Font CreateFont(string fontFamily, float size, FontStyle style = FontStyle.Regular)
        {
            return _fontCollection.CreateFont(fontFamily, size, style);
        }

        public static IImageProcessingContext DrawResource(this IImageProcessingContext context, string resourceName)
        {
            using (var resourceStream = _assembly.GetManifestResourceStream(resourceName))
            using (var resourceImage = Image.Load(resourceStream))
                return context.DrawImage(resourceImage, Point.Empty, 1);
        }

        public static IImageProcessingContext DrawText(
            this IImageProcessingContext context,
            string text,
            RectangleF target,
            string fontFamily,
            Color color,
            FontStyle style = FontStyle.Regular,
            bool wordwrap = true,
            float maxFontSize = 100,
            AnchorPositionMode anchorPoint = AnchorPositionMode.TopLeft,
            HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment verticalAlignment = VerticalAlignment.Top,
            float horizontalPadding = 10,
            float verticalPadding = 10)
        {
            #region [prepare]

            text = text.Replace("\n", " ");
            var font = _fontCollection.CreateFont(fontFamily, maxFontSize, style);

            var rec = new RectangleF(
                target.X + horizontalPadding,
                target.Y + verticalPadding,
                target.Width - (horizontalPadding * 2),
                target.Height - (verticalPadding * 2));

            var options = new DrawingOptions
            {
                TextOptions = new TextOptions
                {
                    HorizontalAlignment = horizontalAlignment,
                    VerticalAlignment = verticalAlignment,
                },
            };

            var location = anchorPoint switch
            {
                AnchorPositionMode.Top => new PointF(rec.Left + (rec.Width / 2), rec.Top),
                AnchorPositionMode.TopRight => new PointF(rec.Right, rec.Top),
                AnchorPositionMode.Left => new PointF(rec.Left, rec.Top + (rec.Height / 2)),
                AnchorPositionMode.Center => new PointF(rec.Left + (rec.Width / 2), rec.Top + (rec.Height / 2)),
                AnchorPositionMode.Right => new PointF(rec.Right, rec.Top + (rec.Height / 2)),
                AnchorPositionMode.BottomLeft => new PointF(rec.Left, rec.Bottom),
                AnchorPositionMode.Bottom => new PointF(rec.Left + (rec.Width / 2), rec.Bottom),
                AnchorPositionMode.BottomRight => new PointF(rec.Right, rec.Bottom),
                _ => new PointF(rec.Left, rec.Top),
            };

            #endregion

            #region [single line]

            if (wordwrap == false)
            {
                var size = TextMeasurer.Measure(text, new RendererOptions(font));
                var scalingFactor = Math.Min(rec.Width / size.Width, rec.Height / size.Height);
                font = new Font(font, scalingFactor * font.Size);
                return context.DrawText(options, text, font, color, location);
            }

            #endregion

            #region [multi line]

            var words = text.Split(" ");
            var lines = new List<string>();
            var line = string.Empty;
            var lineHeight = 0f;

            for (var i = 0; i < words.Length; i++)
            {
                var temp = line + " " + words[i];
                var size = TextMeasurer.Measure(temp.Trim(), new RendererOptions(font));

                if (size.Height > lineHeight)
                    lineHeight = size.Height;

                if (size.Width < rec.Width)
                {
                    line += " " + words[i];
                }
                else
                {
                    lines.Add(line.Trim());
                    line = words[i];

                    var currentHeight = Math.Ceiling(lines.Count * lineHeight);
                    if (currentHeight > Math.Floor(rec.Height - lineHeight))
                    {
                        lines = new List<string>();
                        line = string.Empty;
                        i = -1;
                        lineHeight = 0;
                        font = new Font(font, font.Size - 1);
                    }
                }
            }

            lines.Add(line.Trim());
            foreach (var lineText in lines)
            {
                context.DrawText(options, lineText, font, color, location);
                location.Y += lineHeight;
            }

            return context;

            #endregion
        }

        public static IImageProcessingContext DrawRectangle(this IImageProcessingContext context, RectangleF rectangle, Color color, float thickness = 1)
        {
            var points = new PointF[]
            {
                new (rectangle.Left, rectangle.Top),
                new (rectangle.Right, rectangle.Top),
                new (rectangle.Right, rectangle.Bottom),
                new (rectangle.Left, rectangle.Bottom),
                new (rectangle.Left, rectangle.Top),
            };

            return context.DrawPolygon(color, thickness, points);
        }
    }
}
