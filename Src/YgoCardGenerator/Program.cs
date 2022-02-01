using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Configuration;
using YgoCardGenerator.Commands;

namespace YgoCardGenerator
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder().Build();

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new GeneratorCore.AutofacModule(config));
            var container = containerBuilder.Build();

            await AppCommand.Do(container, args);
            System.Console.ReadKey();
        }
    }
}
