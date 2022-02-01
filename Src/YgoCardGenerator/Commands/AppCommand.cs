using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using TripleSix.Core.Extensions;
using YgoCardGenerator.Types;

namespace YgoCardGenerator.Commands
{
    public abstract class AppCommand
    {
        protected static readonly string AppCode = Assembly.GetExecutingAssembly().GetName().Name;
        private static readonly (string command, string description)[] _usages = new (string command, string description)[]
        {
            new () { command = $"<command>", description = "run <command>" },
            new () { command = $"<command> -h", description = "quick help on <command>" },
        };

        private readonly string _flagBeginKey = "-";
        private readonly IContainer _container;
        private readonly string[] _args;

        protected AppCommand(IContainer container, string[] args)
        {
            _container = container;
            _args = args.IsNullOrEmpty()
                ? new string[0]
                : args.Where(x => x.IsNotNullOrWhiteSpace()).ToArray();
        }

        public string Code
        {
            get
            {
                var cmdName = GetType().Name;
                return cmdName.Substring(0, cmdName.Length - 7)
                    .ToKebabCase();
            }
        }

        public virtual string Description { get; }

        public virtual string[] Usages => null;

        public virtual CommandOption[] Options => null;

        public static async Task Do(IContainer container, string[] args)
        {
            var commandCode = args.IsNotNullOrEmpty() ? args[0] : null;
            if (commandCode.IsNullOrWhiteSpace())
            {
                Help(container);
                return;
            }

            var commandTypeName = commandCode.ToCamelCase() + "Command";
            commandTypeName = char.ToUpper(commandTypeName[0]) + commandTypeName[1..];
            var commandType = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.IsPublic)
                .Where(x => x.IsClass)
                .Where(x => x.IsSubclassOf(typeof(AppCommand)))
                .Where(x => x.Name == commandTypeName)
                .FirstOrDefault();
            if (commandType is null)
                throw new ArgumentException($"'{commandCode}' is not supported.");

            var command = Activator.CreateInstance(commandType, new object[] { container, args }) as AppCommand;
            if (command.GetArg("-h") is not null)
                command.Help();
            else
                await command.Do();
        }

        public static void Help(IContainer container)
        {
            var version = Assembly.Load("GeneratorCore").GetName().Version.ToString(3);

            Console.WriteLine($"Yu-Gi-Oh! Card Generator (v{version})");
            Console.WriteLine();

            Console.WriteLine($"Usage:");
            foreach (var usage in _usages)
                Console.WriteLine($"  {AppCode + " " + usage.command,-35} {usage.description}");
            Console.WriteLine();

            Console.WriteLine($"List commands:");
            var commands = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.IsPublic)
                .Where(x => x.IsClass)
                .Where(x => x.IsSubclassOf(typeof(AppCommand)));
            foreach (var commandType in commands)
            {
                var command = Activator.CreateInstance(commandType, new[] { container, null }) as AppCommand;
                Console.WriteLine($"  {command.Code.PadRight(10)} {command.Description}");
            }
        }

        public virtual Task Do()
        {
            var requireOptions = Options?.Where(x => x.IsRequired);
            if (requireOptions.IsNotNullOrEmpty())
            {
                foreach (var option in requireOptions)
                {
                    if (GetOptionValue(option).IsNullOrWhiteSpace())
                        throw new ArgumentException($"option '{option.Name}' is missing or is invalid", option.Name);
                }
            }

            return Task.CompletedTask;
        }

        public virtual void Help()
        {
            var requireOptions = Options?.Where(x => x.IsRequired);
            var usageRequire = string.Empty;
            if (requireOptions.IsNotNullOrEmpty())
            {
                usageRequire = " ";
                foreach (var option in requireOptions)
                    usageRequire += $"<{option.Name}>";
            }

            var usageOption = string.Empty;
            if (Options is not null && Options.Any(x => x.Flags.IsNotNullOrEmpty()))
                usageOption = " [options...]";

            Console.WriteLine(Description);
            Console.WriteLine();

            Console.WriteLine($"Usage:");
            Console.WriteLine($"  {AppCode} {Code}{usageRequire}{usageOption}");
            if (Usages.IsNotNullOrEmpty())
            {
                foreach (var usage in Usages)
                    Console.WriteLine($"  {AppCode} {usage}");
            }

            Console.WriteLine();
            if (Options.IsNotNullOrEmpty())
            {
                Console.WriteLine($"Options:");
                foreach (var option in Options.OrderByDescending(x => x.IsRequired))
                {
                    if (option.IsRequired)
                    {
                        Console.WriteLine($"  {"<" + option.Name + ">",-20} {option.Description}");
                    }
                    else
                    {
                        Console.WriteLine($"  {"[" + string.Join("|", option.Flags.Select(x => _flagBeginKey + x)) + "]",-20} {option.Description}");
                        if (option.DefaultValue.IsNotNullOrWhiteSpace())
                            Console.WriteLine($"{"default: " + option.DefaultValue,39}");
                    }
                }
            }
        }

        protected TService Resolve<TService>()
        {
            return _container.Resolve<TService>();
        }

        protected string GetOptionValue(CommandOption option)
        {
            if (option.IsRequired)
            {
                var index = Array.FindIndex(Options, x => x.Name == option.Name);
                return GetArg(index + 1);
            }

            foreach (var flag in option.Flags)
            {
                var value = GetArg(_flagBeginKey + flag);
                if (value.IsNotNullOrWhiteSpace())
                    return value;
            }

            return option.DefaultValue;
        }

        private string GetArg(int index)
        {
            if (_args.Length <= index) return null;

            var value = _args[index];
            if (value.StartsWith(_flagBeginKey)) value = null;
            return value;
        }

        private string GetArg(string flag)
        {
            var index = Array.IndexOf(_args, flag);
            if (index == -1) return null;

            var value = GetArg(index + 1) ?? string.Empty;
            if (value.StartsWith(_flagBeginKey)) value = string.Empty;
            return value;
        }
    }
}
