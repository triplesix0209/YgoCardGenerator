using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GeneratorCore.Dto;
using GeneratorCore.Interfaces;
using ImageMagick;
using Tomlyn;
using TripleSix.Core.Helpers;
using TripleSix.Core.Services;

namespace GeneratorCore.Services
{
    public abstract class BaseComposeService : BaseService,
        IComposeService
    {
        public abstract string Template { get; }

        public abstract Task Write(CardModelDto model, string outputPath, CardSetDto setConfig);

        public virtual async Task Write(ComposeDataDto input, string outputFilename, CardSetDto setConfig)
        {
            if (setConfig is null || !setConfig.ComposeSilence)
                Console.WriteLine($"Generate card: {input.Code}...");

            if (input.TryValidate().Count > 0)
                throw new AppException(AppExceptions.CardInputInvalid);

            if (outputFilename.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                throw new AppException(AppExceptions.ArgumentInvalid, args: nameof(outputFilename));

            var marco = await LoadMarco(input, setConfig);
            input.PendulumEffect = ApplyMarco(marco, input.PendulumEffect);
            input.Effect = ApplyMarco(marco, input.Effect);
            input.Flavor = ApplyMarco(marco, input.Flavor);
        }

        protected virtual MagickImage GenerateCard(ComposeDataDto input, CardSetDto setConfig)
        {
            return new MagickImage(MagickColors.Transparent, 694, 1013);
        }

        protected virtual string GetResource(params string[] names)
        {
            var resourceName = string.Join(".", names);
            resourceName = string.Join(".", "GeneratorCore.Resources", Template, resourceName, "png");
            return resourceName;
        }

        protected virtual async Task<Dictionary<string, string>> LoadMarco(ComposeDataDto input, CardSetDto setConfig)
        {
            var result = new Dictionary<string, string>();
            result.Add("CARD_NAME", "\"" + input.Name + "\"");

            foreach (var marcoPath in setConfig.Marcos)
            {
                var marcos = Toml.ToModel(await File.ReadAllTextAsync(Path.Join(setConfig.BasePath, marcoPath)));
                foreach (var item in marcos)
                    result.Add(item.Key, item.Value.ToString());
            }

            foreach (var item in result)
                result[item.Key] = ApplyMarco(result, item.Value);
            return result;
        }

        protected string ApplyMarco(Dictionary<string, string> marco, string text)
        {
            if (text.IsNullOrWhiteSpace()) return null;

            var result = text;
            foreach (var item in marco)
                result = result.Replace("{" + item.Key + "}", item.Value);
            return result;
        }
    }
}
