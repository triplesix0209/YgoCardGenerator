using System.IO;
using System.Threading.Tasks;
using Autofac;
using GeneratorCore.Dto;
using GeneratorCore.Enums;
using GeneratorCore.Helpers;
using GeneratorCore.Services;
using Tomlyn;
using TripleSix.Core.Helpers;
using YgoCardGenerator.Types;

namespace YgoCardGenerator.Commands
{
    public class ComposeCommand : AppCommand
    {
        public ComposeCommand(string[] args)
            : base(args)
        {
        }

        public IComponentContext Container { get; set; }

        public override string Description => "Generate card images";

        public override CommandOption[] Options => new CommandOption[]
        {
            new ("cardSet") { Description = "path to card set to generate (TOML)" },
            new (new[] { "ouput", "o" }) { Description = "output path" },
        };

        public override async Task Do()
        {
            await base.Do();

            var cardSetPath = GetOptionValue(Options[0]);
            var outputPath = GetOptionValue(Options[1]);
            var basePath = Path.GetDirectoryName(cardSetPath);

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            var setConfig = Toml.ToModel<CardSetDto>(await File.ReadAllTextAsync(cardSetPath));
            foreach (var packPath in setConfig.Packs)
            {
                var basePackPath = Path.GetDirectoryName(Path.Join(basePath, packPath));
                var cards = Toml.ToModel(await File.ReadAllTextAsync(Path.Join(basePath, packPath)));
                foreach (var card in cards.Values)
                {
                    var model = Toml.ToModel<CardModelDto>(Toml.FromModel(card));
                    var input = new ComposeDataDto
                    {
                        Code = model.Code,
                        Name = model.Name,
                        Rarity = model.Rarity,
                        CardType = model.Type.FirstMatchCardType(CardTypes.Monster, CardTypes.Spell, CardTypes.Trap),
                        Flavor = model.Flavor?.Trim(),
                        Effect = model.Effect?.Trim(),
                        PendulumEffect = model.PendulumEffect?.Trim(),
                    };

                    input.ArtworkPath = Path.Join(basePackPath, input.Code);
                    if (File.Exists(input.ArtworkPath + ".png"))
                        input.ArtworkPath += ".png";
                    else
                        input.ArtworkPath += ".jpg";

                    if (input.IsSpellTrap)
                    {
                        input.SpellType = model.Type.FirstMatchCardType(SpellTypes.Normal, EnumHelper.GetValues<SpellTypes>());
                    }
                    else
                    {
                        //input.Attribute = model.Attribute.FirstMatchCardType(null, EnumHelper.GetValues<MonsterAttributes>());
                    }

                    var outputFilename = Path.Join(outputPath, input.Code + ".png");
                    await Container.Resolve<ProxyComposeService>()
                        .Write(input, outputFilename, setConfig);
                }
            }

            //var input = new ComposeDataDto
            //{
            //    Name = "Supreme King Dragon Odd-Eyes",
            //    CardType = CardTypes.Monster,
            //    MonsterType = new[] { MonsterTypes.Effect, MonsterTypes.Pendulum },
            //    Attribute = new[] { MonsterAttributes.Dark },
            //    Race = new[] { MonsterRaces.Dragon },
            //    ArtworkPath = @"C:\Users\tripl\Pictures\avatar.jpg",
            //    Level = 8,
            //    LeftScale = 4,
            //    RightScale = 4,
            //    ATK = 2500,
            //    DEF = 2000,
            //    PendulumEffect = "You can Tribute 1 \"Supreme King Dragon\" monster; destroy this card, and if you do, add 1 Pendulum Monster with 1500 or less ATK from your Deck to your hand.",
            //    Effect = "You can Tribute 2 \"Supreme King Dragon\" monsters; Special Summon this card from your hand. If your Pendulum Monster battles an opponent's monster, any battle damage it inflicts to your opponent is doubled. During the Battle Phase (Quick Effect): You can Tribute this card; Special Summon up to 2 face-up \"Supreme King Dragon\" and/or \"Supreme King Gate\" Pendulum Monsters from your Extra Deck in Defense Position, except \"Supreme King Dragon Odd - Eyes\".",
            //};
        }
    }
}
