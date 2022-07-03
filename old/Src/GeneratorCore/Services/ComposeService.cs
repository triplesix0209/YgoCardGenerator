using System.IO;
using System.Threading.Tasks;
using GeneratorCore.Dto;
using GeneratorCore.Helpers;
using GeneratorCore.Interfaces;
using ImageMagick;
using TripleSix.Core.Services;

namespace GeneratorCore.Services
{
    public abstract class ComposeService : BaseService,
        IComposeService
    {
        public abstract string Template { get; }

        public virtual Task Compose(CardModelDto model, string outputPath, CardSetDto setConfig)
        {
            return Compose(model.ToDataDto(), outputPath, setConfig);
        }

        public virtual async Task Compose(CardDataDto data, string outputPath, CardSetDto setConfig)
        {
            if (data.TryValidate().Count > 0)
                throw new AppException(AppExceptions.CardInputInvalid);

            if (outputPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                throw new AppException(AppExceptions.ArgumentInvalid, args: nameof(outputPath));

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            var marco = await setConfig.LoadMarco(data);
            data.PendulumEffect = data.PendulumEffect.ApplyMarco(marco);
            data.Effect = data.Effect.ApplyMarco(marco);
            data.Flavor = data.Flavor.ApplyMarco(marco);
        }

        protected virtual MagickImage GenerateCard(CardDataDto data, CardSetDto setConfig)
        {
            return new MagickImage(MagickColors.Transparent, 694, 1013);
        }

        protected virtual string GetResource(params string[] names)
        {
            var resourceName = string.Join(".", names);
            resourceName = string.Join(".", "GeneratorCore.Resources", Template, resourceName, "png");
            return resourceName;
        }
    }
}
