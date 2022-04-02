using System.IO;
using System.Linq;
using GeneratorCore.Enums;
using GeneratorCore.Helpers;
using TripleSix.Core.Dto;
using TripleSix.Core.Helpers;

namespace GeneratorCore.Dto
{
    public class CardModelDto : DataDto
    {
        public string BasePath { get; set; }

        public int Id { get; set; }

        public int? Alias { get; set; }

        public string Set { get; set; }

        public string Name { get; set; }

        public CardLimits CardLimit { get; set; } = CardLimits.Custom;

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

        public string[] Strings { get; set; }

        public bool Compose { get; set; } = true;

        public CardDataDto ToDataDto()
        {
            var result = new CardDataDto
            {
                Id = Id,
                Alias = Alias,
                Set = Set,
                Name = Name,
                CardLimit = CardLimit,
                Rarity = Rarity,
                CardType = Type.FirstMatchCardType(CardTypes.None),
                Strings = Strings,
            };

            result.ArtworkPath = Path.Combine(BasePath, "artwork", result.Id.ToString());
            if (File.Exists(result.ArtworkPath + ".png"))
                result.ArtworkPath += ".png";
            else
                result.ArtworkPath += ".jpg";

            if (result.IsSpellTrap)
            {
                result.SpellType = Type.FirstMatchCardType(SpellTypes.Normal);
                result.Effect = Effect?.Trim();
            }
            else if (result.IsMonster)
            {
                result.MonsterType = Type.MatchCardType<MonsterTypes>();
                if (!result.MonsterType.Any()) result.MonsterType = new[] { MonsterTypes.Normal };
                result.Attribute = Attribute.MatchCardType<MonsterAttributes>();
                result.Race = Race.MatchCardType<MonsterRaces>();

                result.LeftScale = LeftScale ?? Scale;
                result.RightScale = RightScale ?? Scale;
                if (result.IsMonsterType(MonsterTypes.Link))
                    result.LinkRating = Link ?? Level;
                else if (result.IsMonsterType(MonsterTypes.Xyz))
                    result.Rank = Rank ?? Level;
                else
                    result.Level = Level;

                result.Atk = Atk.IsNotNullOrWhiteSpace() && Atk.Trim() != "?" ? int.Parse(Atk) : null;
                result.Def = Def.IsNotNullOrWhiteSpace() && Def.Trim() != "?" ? int.Parse(Def) : null;
                result.Flavor = Flavor?.Trim();
                result.Effect = Effect?.Trim();
                result.PendulumEffect = PendulumEffect?.Trim();
                result.PendulumSize = PendulumSize;
            }

            if (result.IsLink) result.LinkArrow = LinkArrow.MatchCardType<LinkArrows>();
            return result;
        }
    }
}
