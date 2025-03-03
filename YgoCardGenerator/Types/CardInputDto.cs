﻿namespace YgoCardGenerator.Types
{
    public class CardInputDto : Dto
    {
        public string? Key { get; set; }

        public int Id { get; set; }

        public int? Alias { get; set; }

        public string? Set { get; set; }

        public string? Name { get; set; }

        public string? NameTextColor { get; set; }

        public string? NameShadowColor { get; set; }

        public CardTemplates Template { get; set; } = CardTemplates.Proxy;

        public CardRarities Rarity { get; set; } = CardRarities.Common;

        public string? CardLimit { get; set; }

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

        public bool ShowLevel { get; set; } = true;

        public bool ShowRank { get; set; } = true;

        public string? Atk { get; set; }

        public string? Def { get; set; }

        public string? Flavor { get; set; }

        public string? Effect { get; set; }

        public string? PendulumEffect { get; set; }

        public PendulumSizes PendulumSize { get; set; } = PendulumSizes.Auto;

        public string[]? Strings { get; set; }

        public bool Compose { get; set; } = true;

        public string? FramePath { get; set; }

        public bool GeneratePic { get; set; } = true;

        public bool GenerateScript { get; set; } = true;

        public CardDataDto ToCardDataDto(string basePath)
        {
            if (Key.IsNullOrWhiteSpace())
                throw new ArgumentNullException(nameof(Key));

            var result = new CardDataDto(Key, basePath)
            {
                Id = Id,
                Alias = Alias,
                Set = Set,
                Name = Name,
                NameTextColor = NameTextColor,
                NameShadowColor = NameShadowColor,
                Template = Template,
                Rarity = Rarity,
                CardLimit = CardLimit.MatchEnum<CardLimits>() ?? new[] { CardLimits.Custom },
                CardType = Type.FirstMatchEnum<CardTypes>() ?? CardTypes.None,
                Strings = Strings,
                ShowLevel = ShowLevel,
                ShowRank = ShowRank,
                GeneratePic = GeneratePic,
                GenerateScript = GenerateScript,
            };

            result.ScriptPath = Path.Combine(basePath, "script", $"{result.Key}.lua");
            result.ArtworkPath = Path.Combine(basePath, "artwork", result.Key.ToString());
            result.ArtworkPath += File.Exists(result.ArtworkPath + ".png") ? ".png" : ".jpg";
            result.FramePath = FramePath.IsNullOrWhiteSpace() ? null : Path.Combine(basePath, FramePath);

            if (result.IsSpellTrap)
            {
                result.SpellType = Type.FirstMatchEnum<SpellTypes>() ?? SpellTypes.Normal;
                result.Effect = Effect?.Trim();
            }
            else if (result.IsMonster)
            {
                result.MonsterType = Type.MatchEnum<MonsterTypes>() ?? new[] { MonsterTypes.Normal };
                result.Attribute = Attribute.MatchEnum<MonsterAttributes>();
                result.Race = Race.MatchEnum<MonsterRaces>();

                if (result.IsMonsterType(MonsterTypes.Link))
                    result.LinkRating = Link ?? Level ?? 0;
                else
                {
                    var firstMonsterType = result.FirstMonsterPrimaryType!.Value;
                    if (firstMonsterType == MonsterTypes.Xyz)
                        result.Rank = Rank ?? Level ?? 0;
                    else
                        result.Level = Level ?? 0;
                }

                result.LeftScale = LeftScale ?? Scale;
                result.RightScale = RightScale ?? Scale;

                result.Atk = !Atk.IsNullOrWhiteSpace() && Atk.Trim() != "?" ? int.Parse(Atk) : null;
                result.Def = !Def.IsNullOrWhiteSpace() && Def.Trim() != "?" ? int.Parse(Def) : null;
                result.Flavor = Flavor?.Trim();
                result.Effect = Effect?.Trim();
                result.PendulumEffect = PendulumEffect?.Trim();
                result.PendulumSize = PendulumSize;

                if (result.IsMonsterType(MonsterTypes.Token))
                    result.GenerateScript = false;
            }

            if (result.IsLink)
                result.LinkArrow = LinkArrow.MatchEnum<LinkArrows>();

            return result;
        }
    }
}
