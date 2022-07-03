using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TripleSix.Core.Helpers;

namespace GeneratorCore.Helpers
{
    public static class CardHelper
    {
        public static TEnum[] MatchCardType<TEnum>(this string inputType, params TEnum[] cardTypes)
                    where TEnum : Enum
        {
            return inputType?.Split(" ", StringSplitOptions.RemoveEmptyEntries)
                .Where(type => cardTypes.Any(cardType => cardType.ToString().ToKebabCase().ToLower() == type.ToKebabCase().ToLower()))
                .Select(type => EnumHelper.Parse<TEnum>(type.ToCamelCase()))
                .ToArray();
        }

        public static TEnum[] MatchCardType<TEnum>(this string inputType)
            where TEnum : Enum
        {
            return MatchCardType(inputType, (TEnum[])EnumHelper.GetValues(typeof(TEnum)));
        }

        public static TEnum FirstMatchCardType<TEnum>(this string inputType, TEnum defaultValue, params TEnum[] cardTypes)
            where TEnum : Enum
        {
            var result = inputType?
                .Split(" ", StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(type => cardTypes.Any(cardType => cardType.ToString().ToKebabCase().ToLower() == type.ToKebabCase().ToLower()));

            if (result is null) return defaultValue;
            return EnumHelper.Parse<TEnum>(result.ToCamelCase());
        }

        public static TEnum FirstMatchCardType<TEnum>(this string inputType, TEnum defaultValue)
            where TEnum : Enum
        {
            return FirstMatchCardType(inputType, defaultValue, (TEnum[])EnumHelper.GetValues(typeof(TEnum)));
        }

        public static string ApplyMarco(this string input, Dictionary<string, string> marco)
        {
            if (input.IsNullOrWhiteSpace()) return null;

            var result = input;
            foreach (var item in marco)
            {
                var regex = new Regex(@"\{" + item.Key + @"(\|[\w-"" ]+)*\}");
                var matches = regex.Matches(result);
                if (matches.Count == 0) continue;

                foreach (Match match in matches)
                {
                    var value = match.Groups.Values.First().Value;
                    value = value[(2 + item.Key.Length)..];
                    if (value.IsNotNullOrWhiteSpace())
                        value = value.Substring(0, value.Length - 1);

                    var text = string.Format(item.Value, value.Split("|"));
                    result = regex.Replace(result, text);
                }
            }

            return result;
        }
    }
}
