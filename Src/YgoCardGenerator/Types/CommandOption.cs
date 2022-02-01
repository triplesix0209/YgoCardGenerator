using System.Linq;
using TripleSix.Core.Extensions;

namespace YgoCardGenerator.Types
{
    public class CommandOption
    {
        public CommandOption(string name)
        {
            Name = name.ToKebabCase();
            Flags = null;
            DefaultValue = null;
            IsRequired = true;
        }

        public CommandOption(string[] flags, string defaultValue = "")
        {
            Name = null;
            Flags = flags.Select(x => x.ToKebabCase()).ToArray();
            DefaultValue = defaultValue;
            IsRequired = false;
        }

        public string Name { get; }

        public string[] Flags { get; }

        public string DefaultValue { get; }

        public bool IsRequired { get; }

        public string Description { get; set; }
    }
}
