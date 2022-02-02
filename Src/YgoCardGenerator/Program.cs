using System.Threading.Tasks;
using Autofac;
using ImageMagick;
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
            containerBuilder.RegisterModule(new AutofacModule(config));
            var container = containerBuilder.Build();

            MagickNET.Initialize();
            await AppCommand.Do(container, args);
        }
    }
}
