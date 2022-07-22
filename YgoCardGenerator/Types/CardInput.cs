namespace YgoCardGenerator.Types
{
    public class CardInput : Dto
    {
        public string? BasePath { get; set; }

        public int Id { get; set; }

        public int? Alias { get; set; }

        public string? Set { get; set; }

        public string? Name { get; set; }

        public CardTemplates Template { get; set; } = CardTemplates.Proxy;

        public CardLimits CardLimit { get; set; } = CardLimits.Custom;

        public CardRarities Rarity { get; set; } = CardRarities.Common;

        public string? Type { get; set; }

        public string? Attribute { get; set; }

        public string? Race { get; set; }

        public int? Level { get; set; }

        public int? Rank { get; set; }

        public int? Link { get; set; }

        public string? LinkArrow { get; set; }

        public int? Scale { get; set; }

        public int? LeftScale { get; set; }

        public int? RightScale { get; set; }

        public string? Atk { get; set; }

        public string? Def { get; set; }

        public string? Flavor { get; set; }

        public string? Effect { get; set; }

        public string? PendulumEffect { get; set; }

        public PendulumSizes PendulumSize { get; set; } = PendulumSizes.Auto;

        public string[]? Strings { get; set; }

        public bool Compose { get; set; } = true;

        public CardData ToCardData()
        {
            var result = new CardData
            {
                BasePath = BasePath,
                Id = Id,
                Alias = Alias,
                Set = Set,
                Name = Name,
                Template = Template,
                CardLimit = CardLimit,
                Rarity = Rarity,
                CardType = Type.FirstMatchEnum<CardTypes>() ?? CardTypes.None,
                Strings = Strings,
            };

            result.ArtworkPath = Path.Combine(BasePath!, "artwork", result.Id.ToString());
            result.ArtworkPath += File.Exists(result.ArtworkPath + ".png") ? ".png" : ".jpg";

            if (result.IsSpellTrap)
            {
                result.SpellType = Type.FirstMatchEnum<SpellTypes>() ?? SpellTypes.Normal;
                result.Effect = Effect?.Trim();
            }
            else if (result.IsMonster)
            {
                result.MonsterType = Type.MatchEnum<MonsterTypes>() ?? new [] { MonsterTypes.Normal };
                result.Attribute = Attribute.MatchEnum<MonsterAttributes>();
                result.Race = Race.MatchEnum<MonsterRaces>();

                if (result.IsMonsterType(MonsterTypes.Link))
                    result.LinkRating = Link ?? Level ?? 0;
                else if (result.IsMonsterType(MonsterTypes.Xyz))
                    result.Rank = Rank ?? Level ?? 0;
                else
                    result.Level = Level ?? 0;

                result.LeftScale = LeftScale ?? Scale;
                result.RightScale = RightScale ?? Scale;

                if (result.IsLink)
                    result.LinkArrow = LinkArrow.MatchEnum<LinkArrows>();

                result.Atk = !Atk.IsNullOrWhiteSpace() && Atk.Trim() != "?" ? int.Parse(Atk) : null;
                result.Def = !Def.IsNullOrWhiteSpace() && Def.Trim() != "?" ? int.Parse(Def) : null;
                result.Flavor = Flavor?.Trim();
                result.Effect = Effect?.Trim();
                result.PendulumEffect = PendulumEffect?.Trim();
                result.PendulumSize = PendulumSize;
            }

            return result;
        }
    }
}
