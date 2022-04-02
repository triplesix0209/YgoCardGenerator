using System;
using System.Collections.Generic;
using System.Linq;
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

        public static string ApplyMarco(this string text, Dictionary<string, string> marco)
        {
            if (text.IsNullOrWhiteSpace()) return null;

            var result = text;
            foreach (var item in marco)
                result = result.Replace("{" + item.Key + "}", item.Value);
            return result;
        }
    }
}
