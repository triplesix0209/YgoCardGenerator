using System.Threading.Tasks;
using Autofac;
using GeneratorCore.Dto;
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
                Name = "Gundog",
                CardType = GeneratorCore.Enums.CardTypes.Monster,
                MonsterType = new[] { GeneratorCore.Enums.MonsterTypes.Normal },
                Attribute = new[] { GeneratorCore.Enums.MonsterAttributes.Light },
                Race = new[] { GeneratorCore.Enums.MonsterRaces.DivineBeast },
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
