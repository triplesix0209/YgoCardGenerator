using System.IO;
using System.Threading.Tasks;
using Autofac;
using GeneratorCore.Dto;
using GeneratorCore.Services;
using Tomlyn;
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
            setConfig.BasePath = basePath;
            foreach (var packPath in setConfig.Packs)
            {
                var basePackPath = Path.GetDirectoryName(Path.Join(basePath, packPath));
                var cards = Toml.ToModel(await File.ReadAllTextAsync(Path.Join(basePath, packPath)));
                foreach (var card in cards.Values)
                {
                    var model = Toml.ToModel<CardModelDto>(Toml.FromModel(card));
                    model.BasePath = basePackPath;
                    await Container.Resolve<ProxyComposeService>()
                        .Write(model, outputPath, setConfig);
                }
            }
        }
    }
}
