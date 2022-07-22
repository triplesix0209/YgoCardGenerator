using YgoCardGenerator.Persistences;

namespace YgoCardGenerator.Commands
{
    public class CompileCommand : AppCommand
    {
        public CompileCommand(string[] args)
            : base(args)
        {
        }

        protected override CommandArgument[] ArgumentSchema => new CommandArgument[]
        {
            new ("set") { Description = "path card set filename to compile (TOML)" },
        };

        public override async Task Do()
        {
            // read card set
            var cardSetFilename = Arguments[0].Value();
            var cardSet = Toml.ToModel<CardSetDto>(await File.ReadAllTextAsync(cardSetFilename!));
            cardSet.BasePath = Path.GetDirectoryName(cardSetFilename);
            cardSet.ValidateAndThrow();

            // read card packs
            foreach (var packPath in cardSet.Packs!)
            {
                var cardBasePath = Path.Combine(cardSet.BasePath!, packPath);
                var inputs = Toml.ToModel(await File.ReadAllTextAsync(Path.Combine(cardBasePath, "pack.toml")));
                foreach (var input in inputs.Values)
                {
                    var cardInput = Toml.ToModel<CardInputDto>(Toml.FromModel(input));
                    cardInput.BasePath = cardBasePath;
                    var cardData = cardInput.ToCardDataDto();
                    cardData.ValidateAndThrow();
                }
            }

            var db = new DataContext();
            await db.Database.EnsureCreatedAsync();
        }
    }
}
