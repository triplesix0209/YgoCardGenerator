using System.Reflection;
using Autofac;
using Microsoft.Extensions.Configuration;
using TripleSix.Core.ModuleAutofac;
using YgoCardGenerator.Commands;

namespace YgoCardGenerator
{
    public class AutofacModule : BaseModule
    {
        private readonly Assembly _assembly = Assembly.GetExecutingAssembly();

        public AutofacModule(IConfiguration configuration)
            : base(configuration)
        {
        }

        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.RegisterAssemblyTypes(_assembly)
                .PublicOnly()
                .Where(t => typeof(AppCommand).IsAssignableFrom(t))
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .InstancePerLifetimeScope()
                .AsSelf();
        }
    }
}
