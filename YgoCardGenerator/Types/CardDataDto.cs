namespace YgoCardGenerator.Types
{
    public class CardDataDto : Dto<CardDataValidator>
    {
        public string Key { get; set; }

        public string PackPath { get; set; }

        public string? ArtworkPath { get; set; }

        public string? FramePath { get; set; }

        public string? ScriptPath { get; set; }

        public int Id { get; set; }

        public int? Alias { get; set; }

        public string? Set { get; set; }

        public string? Name { get; set; }

        public string? NameTextColor { get; set; }

        public string? NameShadowColor { get; set; }

        public CardTemplates Template { get; set; } = CardTemplates.Proxy;

        public CardRarities Rarity { get; set; } = CardRarities.Common;

        public CardTypes CardType { get; set; } = CardTypes.None;

        public CardLimits[]? CardLimit { get; set; }

        public SpellTypes? SpellType { get; set; }

        public MonsterTypes[]? MonsterType { get; set; }

        public MonsterAttributes[]? Attribute { get; set; }

        public MonsterRaces[]? Race { get; set; }

        public LinkArrows[]? LinkArrow { get; set; }

        public string? Flavor { get; set; }

        public string? Effect { get; set; }

        public string? PendulumEffect { get; set; }

        public int? Level { get; set; }

        public int? Rank { get; set; }

        public int? LinkRating { get; set; }

        public int? LeftScale { get; set; }

        public int? RightScale { get; set; }

        public bool ShowLevel { get; set; } = true;

        public bool ShowRank { get; set; } = true;

        public int? Atk { get; set; }

        public int? Def { get; set; }

        public PendulumSizes PendulumSize { get; set; } = PendulumSizes.Auto;

        public string[]? Strings { get; set; }

        public bool GeneratePic { get; set; } = true;

        public bool GenerateScript { get; set; } = true;

        public bool IsSpellTrap => CardType == CardTypes.Spell || CardType == CardTypes.Trap;

        public bool IsMonster => CardType == CardTypes.Monster;

        public bool IsLink => IsSpellType(SpellTypes.Link) || IsMonsterType(MonsterTypes.Link);

        public MonsterTypes[]? MonsterPrimaryTypes => MonsterType?
            .Where(x => new[]
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

        public MonsterTypes[]? MonsterSecondaryTypes => MonsterType?
            .Where(x => !MonsterPrimaryTypes!.Contains(x))
            .ToArray();

        public bool IsSpellType(SpellTypes type) => IsSpellTrap && SpellType == type;

        public bool IsMonsterType(params MonsterTypes[] types) => IsMonster && MonsterType != null && MonsterType.Any(x => types.Contains(x));

        public bool HasLinkArrow(LinkArrows arrows) => IsLink && LinkArrow != null && LinkArrow.Any(x => x == arrows);

        public CardDataDto(string key, string packPath)
        {
            this.Key = key;
            this.PackPath = packPath;
        }
    }

    public class CardDataValidator : AbstractValidator<CardDataDto>
    {
        public CardDataValidator()
        {
            RuleFor(x => x.PackPath)
                .NotEmpty();

            RuleFor(x => x.ArtworkPath)
                .NotEmpty();

            RuleFor(x => x.ScriptPath)
                .NotEmpty();

            RuleFor(x => x.Id)
                .GreaterThan(0);

            RuleFor(x => x.Key)
                .NotEmpty();

            RuleFor(x => x.Name)
                .NotEmpty();

            RuleFor(x => x.SpellType)
                .Must((model, field) => !model.IsSpellTrap || field != null)
                .WithMessage("Spell/Trap type cannot be null");

            RuleFor(x => x.MonsterType)
                .Must((model, field) => !model.IsMonster || field != null)
                .WithMessage("Monster type cannot be null");

            RuleFor(x => x.LinkRating)
                .Must((model, field) => !model.IsMonster || !model.IsLink || (0 <= field && field <= 8))
                .WithMessage("must between 0 - 8");

            RuleFor(x => x.Rank)
                .Must((model, field) => !model.IsMonster || !model.IsMonsterType(MonsterTypes.Xyz) || (0 <= field && field <= 13))
                .WithMessage("must between 0 - 13");

            RuleFor(x => x.Level)
                .Must((model, field) => !model.IsMonster || model.IsMonsterType(MonsterTypes.Xyz) || model.IsLink || (0 <= field && field <= 13))
                .WithMessage("must between 0 - 13");

            RuleFor(x => x.LeftScale)
                .Must((model, field) => !model.IsMonsterType(MonsterTypes.Pendulum) || field == null || field >= 0)
                .WithMessage("must >= 0");

            RuleFor(x => x.RightScale)
                .Must((model, field) => !model.IsMonsterType(MonsterTypes.Pendulum) || field == null || field >= 0)
                .WithMessage("must >= 0");

            RuleFor(x => x.Atk)
                .Must((model, field) => !model.IsMonster || field == null || field >= 0)
                .WithMessage("must >= 0");

            RuleFor(x => x.Def)
                .Must((model, field) => !model.IsMonster || model.IsLink || field == null || field >= 0)
                .WithMessage("must >= 0");
        }
    }
}
