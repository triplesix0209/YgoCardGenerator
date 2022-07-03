using System.Reflection;
using Autofac;
using Microsoft.Extensions.Configuration;
using TripleSix.Core.ModuleAutofac;

namespace GeneratorCore
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

            builder.RegisterAllMapper(_assembly);
            builder.RegisterAllService(_assembly);
        }
    }
}
