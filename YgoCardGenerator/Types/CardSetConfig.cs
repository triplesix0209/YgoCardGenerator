using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace YgoCardGenerator.Types
{
    public class CardSetConfig
    {
        internal const string CardIndexFileName = "card.toml";
        internal const string MarcoFileName = "marco.toml";

        public CardSetConfig(CardSetDto input)
        {
            BasePath = input.BasePath!;
            SetCodes = new Dictionary<string, long>();
            Marcos = new Dictionary<string, string>();
            PicFieldPath = !input.PicFieldPath.IsNullOrWhiteSpace() ? input.PicFieldPath : Path.Combine(BasePath, "pics", "field");

            if (!input.Setcodes.IsNullOrEmpty())
            {
                foreach (var path in input.Setcodes)
                {
                    var data = Toml.ToModel(File.ReadAllText(Path.Combine(input.BasePath!, path)));
                    foreach (var item in data)
                        SetCodes.Add(item.Key, (long)item.Value);
                }
            }
        }

        public string BasePath { get; }

        public string PicFieldPath { get; }

        public string ScriptPath => Path.Combine(BasePath, "script");

        public string PicPath => Path.Combine(BasePath, "pics");

        public string UtilityDirectory => Path.Combine(BasePath, "utility");

        public int CardWidth => 694;

        public int CardHeight => 1013;

        public Dictionary<string, long> SetCodes { get; }

        public Dictionary<string, string> Marcos { get; }

        public async Task LoadMarco(CardDataDto card)
        {
            Marcos.Clear();
            Marcos.Add("CARD_NAME", card.Name!);
            if (!File.Exists(Path.Combine(card.PackPath!, MarcoFileName))) return;

            var data = Toml.ToModel(await File.ReadAllTextAsync(Path.Combine(card.PackPath!, MarcoFileName)));
            foreach (var item in data)
            {
                var marco = item.Value.ToString();
                if (marco.IsNullOrEmpty()) continue;
                Marcos.Add(item.Key, marco);
            }

            foreach (var item in data)
            {
                var text = ApplyMarco(Marcos[item.Key]);
                if (text.IsNullOrWhiteSpace()) continue;
                Marcos[item.Key] = text;
            }
        }

        public string? ApplyMarco([NotNullWhen(false)] string? input)
        {
            if (input.IsNullOrEmpty()) return input;

            var result = input;
            foreach (var marco in Marcos)
            {
                var regex = new Regex(@"\{" + marco.Key + @"(\|[\w-"", ]+)*\}");
                var matches = regex.Matches(result);
                if (matches.Count == 0) continue;

                foreach (Match match in matches)
                {
                    var value = match.Groups.Values.First().Value;
                    value = value[(2 + marco.Key.Length)..];
                    if (!value.IsNullOrWhiteSpace())
                        value = value[..^1];

                    var text = string.Format(marco.Value, value.Split("|"));
                    result = regex.Replace(result, text);
                }
            }

            return result;
        }
    }
}
