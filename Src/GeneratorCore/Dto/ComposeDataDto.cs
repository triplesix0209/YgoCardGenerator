using System.Linq;
using GeneratorCore.Enums;
using TripleSix.Core.Attributes;
using TripleSix.Core.Dto;
using TripleSix.Core.Helpers;

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
        public SpellTypes SpellType { get; set; }

        public MonsterTypes[] MonsterType { get; set; }

        public MonsterAttributes[] Attribute { get; set; }

        public MonsterRaces[] Race { get; set; }

        public string Flavor { get; set; }

        public string Effect { get; set; }

        public string ArtworkPath { get; set; }

        [RangeValidate(0, 13)]
        public int Level { get; set; }

        [MinValidate(0)]
        public int? ATK { get; set; }

        [MinValidate(0)]
        public int? DEF { get; set; }

        public bool IsSpellTrap => CardType == CardTypes.Spell || CardType == CardTypes.Trap;

        public bool IsMonster => CardType == CardTypes.Monster;

        public MonsterTypes[] MonsterPrimaryTypes => MonsterType
            ?.Where(x => new[]
            {
                MonsterTypes.Normal,
                MonsterTypes.Effect,
                MonsterTypes.Ritual,
                MonsterTypes.Fusion,
                MonsterTypes.Synchro,
                MonsterTypes.Xyz,
                MonsterTypes.Link,
            }.Contains(x))
            .OrderByDescending(x => x)
            .ToArray();

        public MonsterTypes[] MonsterSecondaryTypes => MonsterType
            ?.Where(x => new[]
            {
                MonsterTypes.Normal,
                MonsterTypes.Effect,
                MonsterTypes.Ritual,
                MonsterTypes.Fusion,
                MonsterTypes.Synchro,
                MonsterTypes.Xyz,
                MonsterTypes.Pendulum,
                MonsterTypes.Link,
            }.Contains(x) == false)
            .ToArray();

        public bool IsMonsterType(params MonsterTypes[] types) => IsMonster && MonsterType.ContainAny(types);
    }
}
