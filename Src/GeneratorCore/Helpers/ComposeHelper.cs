using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ImageMagick;
using ImageMagick.Formats;
using TripleSix.Core.Helpers;

namespace GeneratorCore.Helpers
{
    public static class ComposeHelper
    {
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

        public static string GetFont(string fontFileName)
        {
            return $"Resources/font/{fontFileName}.ttf";
        }

        public static async Task DrawResource(this MagickImage context, string resourceName)
        {
            using (var resourceStream = _assembly.GetManifestResourceStream(resourceName))
            using (var resource = new MagickImage())
            {
                await resource.ReadAsync(resourceStream);
                context.Composite(resource, Gravity.Center, CompositeOperator.Over);
            }
        }

        public static Task DrawTextLine(
            this MagickImage context,
            string text,
            Point location,
            string fontFamily,
            double fontSize,
            IMagickColor<ushort> color = null,
            int? width = null,
            int? maxWidth = null,
            Gravity gravity = Gravity.Northwest,
            FontStyleType fontStyle = FontStyleType.Normal)
        {
            if (color is null) color = MagickColors.Black;

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
                if (width.HasValue)
                {
                    label.Resize(new MagickGeometry
                    {
                        Width = width.Value,
                        Height = (int)fontSize,
                        IgnoreAspectRatio = true,
                    });
                }

                if (maxWidth.HasValue && label.FontTypeMetrics(text).TextWidth > maxWidth)
                {
                    label.Resize(new MagickGeometry
                    {
                        Width = maxWidth.Value,
                        Height = (int)fontSize,
                        IgnoreAspectRatio = true,
                    });
                }

                context.Composite(label, location.X, location.Y, CompositeOperator.Over);
            }

            return Task.CompletedTask;
        }

        public static Task DrawTextAreaAsync(
            this MagickImage context,
            string text,
            Rectangle targetArea,
            string fontFamily,
            IMagickColor<ushort> color = null,
            double? minFontSize = 10,
            double? maxFontSize = 20,
            Gravity gravity = Gravity.Northwest,
            FontStyleType fontStyle = FontStyleType.Normal,
            double wordSpacing = 0)
        {
            if (color is null) color = MagickColors.Black;

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
                TextInterwordSpacing = wordSpacing,
            };

            using (var caption = new MagickImage($"caption:{text}", settings))
            {
                context.Composite(caption, targetArea.X, targetArea.Y, CompositeOperator.Over);
                return Task.CompletedTask;
            }
        }

        public static MagickImage DrawDebugBorder(this MagickImage context)
        {
            new Drawables()
                .StrokeColor(MagickColors.Red)
                .Line(0, 0, context.Width - 1, 0)
                .Line(context.Width - 1, 0, context.Width - 1, context.Height - 1)
                .Line(context.Width - 1, context.Height - 1, 0, context.Height - 1)
                .Line(0, context.Height - 1, 0, 0)
                .Draw(context);

            return context;
        }

        public static bool ContainCardType(this string inputType, params Enum[] cardTypes)
        {
            var inputTypes = inputType.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            return inputTypes.Any(type =>
                cardTypes.Any(cardType => cardType.ToString().ToKebabCase().ToLower() == type.ToKebabCase().ToLower()));
        }

        public static TEnum FirstMatchCardType<TEnum>(this string inputType, TEnum defaultValue, params Enum[] cardTypes)
            where TEnum : Enum
        {
            var inputTypes = inputType.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            var result = inputTypes.FirstOrDefault(type =>
                cardTypes.Any(cardType => cardType.ToString().ToKebabCase().ToLower() == type.ToKebabCase().ToLower()));

            if (result is null) return defaultValue;
            return EnumHelper.Parse<TEnum>(result);
        }

        public static TEnum FirstMatchCardType<TEnum>(this string inputType, TEnum defaultValue, IEnumerable<TEnum> cardTypes)
            where TEnum : Enum
        {
            return FirstMatchCardType(inputType, defaultValue, cardTypes.ToArray());
        }
    }
}
