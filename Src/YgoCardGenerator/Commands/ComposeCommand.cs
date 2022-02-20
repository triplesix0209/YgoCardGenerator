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
                Name = "Chaos Emperor",
                CardType = CardTypes.Monster,
                MonsterType = new[] { MonsterTypes.Effect, },
                Attribute = new[] { MonsterAttributes.Light },
                Race = new[] { MonsterRaces.WingedBeast },
                ArtworkPath = @"G:\My Drive\Personal\avatar.jpg",
                Level = 4,
                ATK = 1500,
                DEF = 500,
                Effect = "During damage calculation, if your opponent's monster attacks (Quick Effect): You can discard this card; you take no battle damage from that battle.",
            };

            await Container.Resolve<ProxyComposeService>()
                .Write(input, output);
        }
    }
}
