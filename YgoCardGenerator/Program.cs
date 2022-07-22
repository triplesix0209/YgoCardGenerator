using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YgoCardGenerator.Commands;

namespace YgoCardGenerator
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information)
                    .AddConsole();
            });

            var commandTypes = RegisterCommand(serviceCollection, args);
            var commandType = commandTypes.FirstOrDefault(x => x.Name.ToLower() == args[0] + "command");
            if (commandType == null)
                throw new ArgumentException($"command \"{args[0]}\" not found");

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var command = (AppCommand)serviceProvider.GetRequiredService(commandType);
            await command.Do();
        }
        
        static Type[] RegisterCommand(IServiceCollection serviceCollection, string[] args)
        {
            var commandTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.IsSubclassOf(typeof(AppCommand)))
                .Where(x => !x.IsAbstract)
                .Where(x => x.IsPublic);

            foreach (var commandType in commandTypes)
            {
                serviceCollection.AddSingleton(commandType, p => Activator.CreateInstance(commandType, new object[]
                {
                    args,
                    p.GetRequiredService<ILogger<Program>>()
                })!);
            }

            return commandTypes.ToArray();
        }
    }
}