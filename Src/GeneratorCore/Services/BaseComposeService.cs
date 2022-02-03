using System.IO;
using System.Threading.Tasks;
using GeneratorCore.Dto;
using GeneratorCore.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TripleSix.Core.Extensions;
using TripleSix.Core.Services;

namespace GeneratorCore.Services
{
    public abstract class BaseComposeService : BaseService,
        IComposeService
    {
        public abstract string Template { get; }

        public virtual Task Write(ComposeDataDto input, string outputFilename)
        {
            if (input.TryValidate().Count > 0)
                throw new AppException(AppExceptions.CardInputInvalid);

            if (outputFilename.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                throw new AppException(AppExceptions.ArgumentInvalid, args: nameof(outputFilename));

            return Task.CompletedTask;
        }

        protected virtual Image GenerateCard(ComposeDataDto input)
        {
            return new Image<Rgba32>(input.Width, input.Height);
        }

        protected virtual string GetResource(params string[] names)
        {
            var resourceName = string.Join(".", names);
            resourceName = string.Join(".", "GeneratorCore.Resources", Template.ToKebabCase(), resourceName, "png");
            return resourceName;
        }
    }
}
