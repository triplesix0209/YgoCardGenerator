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
            var cardSetFilename = Arguments[0].Value();
            if (cardSetFilename.IsNullOrWhiteSpace() || !File.Exists(cardSetFilename))
                throw new Exception("card set is not specify or not found.");

            var cardSet = Toml.ToModel<CardSet>(await File.ReadAllTextAsync(cardSetFilename));
            cardSet.BasePath = Path.GetDirectoryName(cardSetFilename);
            cardSet.ValidateAndThrow();
        }
    }
}
