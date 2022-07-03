using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GeneratorCore.Helpers;
using Tomlyn;
using TripleSix.Core.Dto;
using TripleSix.Core.Helpers;

namespace GeneratorCore.Dto
{
    public class CardSetDto : DataDto
    {
        public string SetName { get; set; }

        public string BasePath { get; set; }

        public bool DrawField { get; set; } = true;

        public string[] Packs { get; set; }

        public string[] Marcos { get; set; }

        public string[] Setcodes { get; set; }

        public bool ComposeSilence { get; set; } = false;

        public async Task<Dictionary<string, long>> LoadSetcode()
        {
            var result = new Dictionary<string, long>();

            if (Setcodes.IsNotNullOrEmpty())
            {
                foreach (var path in Setcodes)
                {
                    var setcodes = Toml.ToModel(await File.ReadAllTextAsync(Path.Combine(BasePath, path)));
                    foreach (var item in setcodes)
                        result.Add(item.Key, (long)item.Value);
                }
            }

            return result;
        }

        public async Task<Dictionary<string, string>> LoadMarco(CardDataDto data)
        {
            var result = new Dictionary<string, string>();
            result.Add("CARD_NAME", data.Name);

            if (Marcos.IsNotNullOrEmpty())
            {
                foreach (var path in Marcos)
                {
                    var marcos = Toml.ToModel(await File.ReadAllTextAsync(Path.Combine(BasePath, path)));
                    foreach (var item in marcos)
                        result.Add(item.Key, item.Value.ToString());
                }
            }

            foreach (var item in result)
                result[item.Key] = item.Value.ApplyMarco(result);
            return result;
        }
    }
}
