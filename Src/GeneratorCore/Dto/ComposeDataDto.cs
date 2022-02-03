using GeneratorCore.Enums;
using TripleSix.Core.Attributes;
using TripleSix.Core.Dto;

namespace GeneratorCore.Dto
{
    public class ComposeDataDto : DataDto
    {
        public int Width { get; set; } = 694;

        public int Height { get; set; } = 1013;

        [RequiredValidate]
        [EnumValidate]
        public CardTypes CardType { get; set; }

        [EnumValidate]
        public CardRarities Rarity { get; set; } = CardRarities.Common;

        [RequiredValidate]
        public string Name { get; set; }

        [EnumValidate]
        public MonsterAttributes Attribute { get; set; }

        [EnumValidate]
        public SpellTypes SpellType { get; set; }

        public bool IsSpellTrap => CardType == CardTypes.Spell || CardType == CardTypes.Trap;

        public string Effect { get; set; }

        public string ArtworkPath { get; set; }
    }
}
