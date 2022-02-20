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

        public LinkArrows[] LinkArrow { get; set; }

        public string Flavor { get; set; }

        public string Effect { get; set; }

        public string PendulumEffect { get; set; }

        public string ArtworkPath { get; set; }

        [RangeValidate(0, 13)]
        public int? Level { get; set; }

        [RangeValidate(0, 13)]
        public int? Rank { get; set; }

        [RangeValidate(0, 14)]
        public int? LeftScale { get; set; }

        [RangeValidate(0, 14)]
        public int? RightScale { get; set; }

        [RangeValidate(0, 8)]
        public int? LinkRating { get; set; }

        [MinValidate(0)]
        public int? ATK { get; set; }

        [MinValidate(0)]
        public int? DEF { get; set; }

        public PendulumSizes PendulumSize { get; set; } = PendulumSizes.Auto;

        public bool IsSpellTrap => CardType == CardTypes.Spell || CardType == CardTypes.Trap;

        public bool IsMonster => CardType == CardTypes.Monster;

        public bool IsLink => IsSpellType(SpellTypes.Link) || IsMonsterType(MonsterTypes.Link);

        public MonsterTypes[] MonsterPrimaryTypes => MonsterType
            ?.Where(x => new[]
            {
                MonsterTypes.Token,
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
            ?.Where(x => !MonsterPrimaryTypes.Contains(x))
            .ToArray();

        public bool IsSpellType(SpellTypes type) => IsSpellTrap && SpellType == type;

        public bool IsMonsterType(params MonsterTypes[] types) => IsMonster && MonsterType.ContainAny(types);

        public bool HasLinkArrow(LinkArrows arrows) => IsLink && LinkArrow.ContainAny(arrows);
    }
}
