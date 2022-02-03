using GeneratorCore.Enums;
using System.Linq;
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

        public string Effect { get; set; }

        public string ArtworkPath { get; set; }

        [RangeValidate(0, 13)]
        public int Level { get; set; }

        [MinValidate(0)]
        public int? ATK { get; set; }

        [MinValidate(0)]
        public int? DEF { get; set; }

        public bool IsSpellTrap => CardType == CardTypes.Spell || CardType == CardTypes.Trap;

        public bool IsMonster => new[]
        {
            CardTypes.Normal, CardTypes.Effect, CardTypes.Ritual,
            CardTypes.Fusion, CardTypes.Synchro, CardTypes.Xyz,
            CardTypes.Link,
        }.Contains(CardType);
    }
}
