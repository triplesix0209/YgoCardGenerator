using GeneratorCore.Enums;
using TripleSix.Core.Dto;

namespace GeneratorCore.Dto
{
    public class CardComposeDataDto : DataDto
    {
        public int Width { get; set; } = 694;

        public int Height { get; set; } = 1013;

        public string Template { get; set; } = "proxy";

        public string Rarity { get; set; } = "common";

        public CardTypes Type { get; set; }
    }
}
