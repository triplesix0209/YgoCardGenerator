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
                CardType = GeneratorCore.Enums.CardTypes.Normal,
                Attribute = GeneratorCore.Enums.MonsterAttributes.Light,
                ArtworkPath = @"G:\My Drive\Personal\avatar.jpg",
                Level = 4,
                ATK = 1500,
                DEF = 1000,
                //Effect = "Target 1 monster in either GY; Special Summon it.",
            };

            await Container.Resolve<ProxyComposeService>()
                .Write(input, output);
        }
    }
}
