using Microsoft.Extensions.Logging;
using YgoCardGenerator.Persistences;

namespace YgoCardGenerator.Commands
{
    public class CompileCommand : AppCommand
    {
        public CompileCommand(string[] args, ILogger logger)
            : base(args, logger)
        {
        }

        protected override CommandArgument[] ArgumentSchema => new CommandArgument[]
        {
            new ("set") { Description = "path card set filename to compile (TOML)" },
        };

        public override async Task Do()
        {
            #region [read card data]

            var cardSetFilename = Arguments[0].Value();
            var cardSet = Toml.ToModel<CardSetDto>(await File.ReadAllTextAsync(cardSetFilename!));
            cardSet.BasePath = Path.GetDirectoryName(cardSetFilename);
            cardSet.ValidateAndThrow();

            var cardPacks = new List<(string path, List<CardDataDto> cards)>();
            foreach (var packPath in cardSet.Packs!)
            {
                (string path, List<CardDataDto> cards) cardPack = new()
                {
                    path = Path.Combine(cardSet.BasePath!, packPath),
                    cards = new List<CardDataDto>()
                };
                
                var inputs = Toml.ToModel(await File.ReadAllTextAsync(Path.Combine(cardPack.path, "pack.toml")));
                foreach (var input in inputs.Values)
                {
                    var cardInput = Toml.ToModel<CardInputDto>(Toml.FromModel(input));
                    var cardData = cardInput.ToCardDataDto(cardPack.path);
                    cardData.ValidateAndThrow();
                    cardPack.cards.Add(cardData);
                }

                cardPacks.Add(cardPack);
            }

            #endregion

            // prepare card db
            var db = new DataContext();
            await db.Database.EnsureCreatedAsync();
        }
    }
}
