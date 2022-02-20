using System.Drawing;
using System.IO;
using System.Reflection;
using ImageMagick;
using ImageMagick.Formats;

namespace GeneratorCore.Helpers
{
    public static class ComposeHelper
    {
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

        public static string GetFont(string fontFileName)
        {
            return $"Resources/font/{fontFileName}.ttf";
        }

        public static MagickImage DrawResource(this MagickImage context, string resourceName)
        {
            using (var resourceStream = _assembly.GetManifestResourceStream(resourceName))
            using (var resourceImage = new MagickImage(resourceStream))
                context.Composite(resourceImage, Gravity.Center, CompositeOperator.Over);

            return context;
        }

        public static MagickImage DrawTextLine(
            this MagickImage context,
            string text,
            PointD location,
            string fontFamily,
            IMagickColor<ushort> color,
            double fontSize,
            int? maxWidth = null,
            Gravity gravity = Gravity.Northwest,
            FontStyleType fontStyle = FontStyleType.Normal)
        {
            var settings = new MagickReadSettings()
            {
                Font = GetFont(fontFamily),
                FontPointsize = fontSize,
                FillColor = color,
                StrokeColor = MagickColors.None,
                BackgroundColor = MagickColors.None,
                TextGravity = gravity,
                FontStyle = fontStyle,
            };

            using (var label = new MagickImage($"label:{text}", settings))
            {
                if (maxWidth.HasValue && label.FontTypeMetrics(text).TextWidth > maxWidth)
                {
                    label.Resize(new MagickGeometry
                    {
                        Width = maxWidth.Value,
                        Height = (int)fontSize,
                        IgnoreAspectRatio = true,
                    });
                }

                context.Composite(label, location, CompositeOperator.Over);
            }

            return context;
        }

        public static MagickImage DrawTextArea(
            this MagickImage context,
            string text,
            Rectangle targetArea,
            string fontFamily,
            IMagickColor<ushort> color,
            double? minFontSize = null,
            double? maxFontSize = null,
            Gravity gravity = Gravity.West,
            FontStyleType fontStyle = FontStyleType.Normal)
        {
            var settings = new MagickReadSettings()
            {
                Defines = new CaptionReadDefines() { StartFontPointsize = minFontSize, MaxFontPointsize = maxFontSize },
                Font = GetFont(fontFamily),
                Width = targetArea.Width,
                Height = targetArea.Height,
                FillColor = color,
                StrokeColor = MagickColors.None,
                BackgroundColor = MagickColors.None,
                TextGravity = gravity,
                FontStyle = fontStyle,
            };

            using (var caption = new MagickImage($"caption:{text}", settings))
                context.Composite(caption, targetArea.X, targetArea.Y, CompositeOperator.Over);

            return context;
        }

        public static MagickImage DrawBorder(this MagickImage context, IMagickColor<ushort> color)
        {
            new Drawables()
                .StrokeColor(color)
                .Line(0, 0, context.Width - 1, 0)
                .Line(context.Width - 1, 0, context.Width - 1, context.Height - 1)
                .Line(context.Width - 1, context.Height - 1, 0, context.Height - 1)
                .Line(0, context.Height - 1, 0, 0)
                .Draw(context);

            return context;
        }
    }
}
