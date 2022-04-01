using System;
using System.IO;
using System.Threading.Tasks;
using GeneratorCore.Dto;
using GeneratorCore.Interfaces;
using ImageMagick;
using TripleSix.Core.Helpers;
using TripleSix.Core.Services;

namespace GeneratorCore.Services
{
    public abstract class BaseComposeService : BaseService,
        IComposeService
    {
        public abstract string Template { get; }

        public abstract Task Write(CardModelDto model, string outputPath, CardSetDto setConfig);

        public virtual Task Write(ComposeDataDto input, string outputFilename, CardSetDto setConfig)
        {
            if (setConfig is null || !setConfig.ComposeSilence)
                Console.WriteLine($"Generate card: {input.Code}...");

            if (input.TryValidate().Count > 0)
                throw new AppException(AppExceptions.CardInputInvalid);

            if (outputFilename.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                throw new AppException(AppExceptions.ArgumentInvalid, args: nameof(outputFilename));

            return Task.CompletedTask;
        }

        protected virtual MagickImage GenerateCard(ComposeDataDto input, CardSetDto setConfig)
        {
            return new MagickImage(MagickColors.Transparent, 694, 1013);
        }

        protected virtual string GetResource(params string[] names)
        {
            var resourceName = string.Join(".", names);
            resourceName = string.Join(".", "GeneratorCore.Resources", Template.ToKebabCase(), resourceName, "png");
            return resourceName;
        }
    }
}
