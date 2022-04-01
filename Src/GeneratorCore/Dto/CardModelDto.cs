using GeneratorCore.Enums;
using TripleSix.Core.Dto;

namespace GeneratorCore.Dto
{
    public class CardModelDto : DataDto
    {
        public string BasePath { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public CardRarities Rarity { get; set; } = CardRarities.Common;

        public string Type { get; set; }

        public string Attribute { get; set; }

        public string Race { get; set; }

        public int? Level { get; set; }

        public int? Rank { get; set; }

        public int? Link { get; set; }

        public string LinkArrow { get; set; }

        public int? Scale { get; set; }

        public int? LeftScale { get; set; }

        public int? RightScale { get; set; }

        public string Atk { get; set; }

        public string Def { get; set; }

        public string Flavor { get; set; }

        public string Effect { get; set; }

        public string PendulumEffect { get; set; }

        public PendulumSizes PendulumSize { get; set; } = PendulumSizes.Auto;
    }
}
