using System.Threading.Tasks;
using Autofac;
using GeneratorCore.Dto;
using GeneratorCore.Enums;
using GeneratorCore.Services;
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
            new ("packPath") { Description = "path to card pack to generate" },
            new (new[] { "ouput", "o" }) { Description = "output path" },
            new (new[] { "size", "s" }, "694x1013") { Description = "dimensions of the final card images" },
        };

        public override async Task Do()
        {
            await base.Do();

            var output = GetOptionValue(0);
            var input = new ComposeDataDto
            {
                //Rarity = CardRarities.Gold,
                Name = "Supreme King Dragon Odd-Eyes",
                CardType = CardTypes.Monster,
                MonsterType = new[] { MonsterTypes.Effect, MonsterTypes.Pendulum },
                Attribute = new[] { MonsterAttributes.Dark },
                Race = new[] { MonsterRaces.Dragon },
                ArtworkPath = @"D:\Temp\Supreme King Dragon Odd-Eyes.png",
                Level = 8,
                LeftScale = 4,
                RightScale = 4,
                ATK = 2500,
                DEF = 2000,
                PendulumEffect = "You can Tribute 1 \"Supreme King Dragon\" monster; destroy this card, and if you do, add 1 Pendulum Monster with 1500 or less ATK from your Deck to your hand.",
                Effect = "You can Tribute 2 \"Supreme King Dragon\" monsters; Special Summon this card from your hand. If your Pendulum Monster battles an opponent's monster, any battle damage it inflicts to your opponent is doubled. During the Battle Phase (Quick Effect): You can Tribute this card; Special Summon up to 2 face-up \"Supreme King Dragon\" and/or \"Supreme King Gate\" Pendulum Monsters from your Extra Deck in Defense Position, except \"Supreme King Dragon Odd - Eyes\".",
            };

            await Container.Resolve<ProxyComposeService>()
                .Write(input, output);
        }
    }
}
