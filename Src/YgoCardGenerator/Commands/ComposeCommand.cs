using System.Threading.Tasks;
using Autofac;
using YgoCardGenerator.Types;

namespace YgoCardGenerator.Commands
{
    public class ComposeCommand : AppCommand
    {
        public ComposeCommand(IContainer container, string[] args)
            : base(container, args)
        {
        }

        public override string Description => "Generate card images";

        public override CommandOption[] Options => new CommandOption[]
        {
            new ("packPath") { Description = "path to card pack to generate" },
            new (new[] { "ouput", "o" }) { Description = "output path" },
            new (new[] { "size", "s" }, "421x614") { Description = "dimensions of the final card images" },
        };

        public override async Task Do()
        {
            await base.Do();
        }
    }
}
