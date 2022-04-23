using System;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using GeneratorCore.Dto;
using GeneratorCore.Enums;
using GeneratorCore.Services;
using Tomlyn;
using YgoCardGenerator.Types;

namespace YgoCardGenerator.Commands
{
    public class CompileCommand : AppCommand
    {
        public CompileCommand(string[] args)
            : base(args)
        {
        }

        public IComponentContext Container { get; set; }

        public override string Description => "Compile card";

        public override CommandOption[] Options => new CommandOption[]
        {
            new ("cardSet") { Description = "path to card set to compile (TOML)" },
            new (new[] { "ouput", "o" }) { Description = "output path" },
        };

        public override async Task Do()
        {
            await base.Do();

            var cardSetPath = GetOptionValue(Options[0]);
            var outputPath = GetOptionValue(Options[1]);
            var basePath = Path.GetDirectoryName(cardSetPath);

            var setConfig = Toml.ToModel<CardSetDto>(await File.ReadAllTextAsync(cardSetPath));
            setConfig.BasePath = basePath;

            var outputPicPath = Path.Combine(outputPath, "pics");
            Directory.CreateDirectory(outputPicPath);

            if (setConfig is null || !setConfig.ComposeSilence)
                Console.WriteLine($"Compile card...");
            await Container.Resolve<CompileService>()
                .Compile(outputPath, setConfig);

            foreach (var packPath in setConfig.Packs)
            {
                var basePackPath = Path.GetDirectoryName(Path.Combine(basePath, packPath));
                var cards = Toml.ToModel(await File.ReadAllTextAsync(Path.Combine(basePath, packPath)));
                foreach (var card in cards.Values)
                {
                    var model = Toml.ToModel<CardModelDto>(Toml.FromModel(card));
                    model.BasePath = basePackPath;
                    if (!model.Compose) continue;

                    if (!setConfig.ComposeSilence)
                        Console.WriteLine($"Compose card: {model.Id}...");

                    if (model.Template == CardTemplates.Artwork)
                        await Container.Resolve<ComposeArtworkService>().Compose(model, outputPicPath, setConfig);
                    else
                        await Container.Resolve<ComposeProxyService>().Compose(model, outputPicPath, setConfig);
                }
            }
        }
    }
}
