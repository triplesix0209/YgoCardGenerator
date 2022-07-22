using Microsoft.Extensions.Logging;

namespace YgoCardGenerator.Commands
{
    public abstract class AppCommand
    {
        protected AppCommand(string[] args, ILogger logger)
        {
            args = (args == null || args.Length == 0)
                ? Array.Empty<string>()
                : args.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            Logger = logger;

            var arguments = new List<CommandArgument>();
            foreach (var schema in ArgumentSchema)
            {
                var keys = new List<string>() { schema.Key };
                if (schema.Flags != null && schema.Flags.Length > 0) keys.AddRange(schema.Flags);

                var index = -1;
                foreach (var key in keys)
                {
                    index = Array.FindIndex(args, x => x.StartsWith("-" + key));
                    if (index != -1) break;
                }

                string? value = null;
                if (index == -1)
                {
                    if (schema.IsRequired)
                        throw new ArgumentException($"\"{schema.Key}\" is is required");
                    else
                        value = schema.DefaultValue;
                }
                else if (args[index].Contains('='))
                {
                    value = args[index].Split('=')[1];
                }
                arguments.Add(new CommandArgument(schema, value));

            }
            Arguments = arguments.ToArray();
        }

        protected ILogger Logger { get; }

        protected abstract CommandArgument[] ArgumentSchema { get; }

        public string Code => GetType().Name[..^7].ToKebabCase();

        public CommandArgument[] Arguments { get; }

        public abstract Task Do();
    }
}
