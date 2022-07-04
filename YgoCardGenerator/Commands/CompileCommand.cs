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
            new ("set") { Description = "path to card set to compile (TOML)" },
            new ("output", "o") { Description = "output path" },
        };

        public override async Task Do()
        {
            var setPath = Arguments[0].Value();
            var outputPath = Arguments[0].Value();
        }
    }
}
