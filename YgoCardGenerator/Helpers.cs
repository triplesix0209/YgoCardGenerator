using System.Text.RegularExpressions;

namespace YgoCardGenerator
{
    public static class Helpers
    {
        public static string ToKebabCase(this string text)
        {
            var texts = Regex.Replace(text, @"([a-z0-9])([A-Z])", "$1 $2")
                .Split(' ', '-', '_')
                .Select(x => x.Trim())
                .Where(x => x.Length > 0);
            return string.Join("-", texts).ToLower();
        }
    }
}
