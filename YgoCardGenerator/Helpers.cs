using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using YgoCardGenerator.Attribtues;

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

        public static string ToSnakeSpaceCase(this string text)
        {
            return string.Join("_", SplitCase(text)).ToLower();
        }

        public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? text)
        {
            return string.IsNullOrWhiteSpace(text);
        }

        public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? list)
        {
            return list == null || !list.Any();
        }

        public static TEnum[]? MatchEnum<TEnum>([NotNullWhen(false)] this string? input, params TEnum[] enumValues)
                    where TEnum : struct, Enum
        {
            if (input.IsNullOrWhiteSpace()) return default;
            var inputValues = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);

            var result = new List<TEnum>();
            foreach (var inputValue in inputValues)
            {
                var enumValue = enumValues.FirstOrDefault(enumValue => inputValue.Equals(enumValue.ToString().ToKebabCase(), StringComparison.CurrentCultureIgnoreCase));
                if (!Enum.IsDefined(enumValue)) continue;
                result.Add(enumValue);
            }

            return result.ToArray();
        }

        public static TEnum[]? MatchEnum<TEnum>([NotNullWhen(false)] this string? input)
            where TEnum : struct, Enum
        {
            return MatchEnum(input, Enum.GetValues<TEnum>());
        }

        public static TEnum? FirstMatchEnum<TEnum>([NotNullWhen(false)] this string? input, params TEnum[] enumValues)
           where TEnum : struct, Enum
        {
            if (input.IsNullOrWhiteSpace()) return default;
            var inputValues = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);

            foreach (var inputValue in inputValues)
                foreach (var enumValue in enumValues)
                {
                    if (inputValue.Equals(enumValue.ToString().ToKebabCase(), StringComparison.CurrentCultureIgnoreCase))
                        return enumValue;
                }

            return default;
        }

        public static TEnum? FirstMatchEnum<TEnum>([NotNullWhen(false)] this string? input)
           where TEnum : struct, Enum
        {
            return FirstMatchEnum(input, Enum.GetValues<TEnum>());
        }

        public static string? GetEnumText(Type enumType, object value)
        {
            var valueName = value.ToString();
            if (valueName is null) return null;
            valueName = Enum.Parse(enumType, valueName).ToString();
            if (valueName is null) return null;
            var memberInfo = enumType.GetMember(valueName).FirstOrDefault();
            if (memberInfo is null) return null;

            var info = memberInfo.GetCustomAttribute<EnumInfoAttribute>();
            return info is null || info.Text.IsNullOrWhiteSpace() ? valueName : info.Text;
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
