using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace YgoCardGenerator
{
    public static class Helpers
    {
        public static string ToCamelCase(this string text)
        {
            var texts = SplitCase(text).Select(x => char.ToUpper(x[0]) + x[1..]);
            var result = string.Join(string.Empty, texts);
            result = char.ToLower(result[0]) + result[1..];

            return result;
        }

        public static string ToKebabCase(this string text)
        {
            return string.Join("-", SplitCase(text)).ToLower();
        }

        public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? text)
        {
            return string.IsNullOrWhiteSpace(text);
        }

        public static TEnum[]? MatchEnum<TEnum>([NotNullWhen(false)] this string? input, params TEnum[] enumValues)
                    where TEnum : Enum
        {
            if (input.IsNullOrWhiteSpace()) return default;
            var inputValues = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);

            return enumValues
                .Where(enumValue => inputValues.Any(value => value.Equals(enumValue.ToString(), StringComparison.CurrentCultureIgnoreCase)))
                .ToArray();
        }

        public static TEnum[]? MatchEnum<TEnum>([NotNullWhen(false)] this string? input)
            where TEnum : struct, Enum
        {
            return MatchEnum(input, Enum.GetValues<TEnum>());
        }

        public static TEnum? FirstMatchEnum<TEnum>([NotNullWhen(false)] this string? input, params TEnum[] enumValues)
           where TEnum : Enum
        {
            if (input.IsNullOrWhiteSpace()) return default;
            var inputValues = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);

            foreach (var inputValue in inputValues)
            {
                var enumValue = enumValues.FirstOrDefault(x => x.ToString()!.Equals(inputValue, StringComparison.CurrentCultureIgnoreCase));
                if (enumValue != null) return enumValue;
            }

            return default;
        }

        public static TEnum? FirstMatchEnum<TEnum>([NotNullWhen(false)] this string? input)
           where TEnum : struct, Enum
        {
            return FirstMatchEnum(input, Enum.GetValues<TEnum>());
        }

        private static string[] SplitCase(string text)
        {
            return Regex.Replace(text, @"([a-z0-9])([A-Z])", "$1 $2")
                .Split(' ', '-', '_')
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToArray();
        }
    }
}
