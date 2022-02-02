using System.Reflection;
using System.Threading.Tasks;
using GeneratorCore.Dto;
using ImageMagick;
using TripleSix.Core.Services;

namespace GeneratorCore.Services
{
    public class CardComposeService : BaseService,
        ICardComposeService
    {
        private readonly Assembly _assembly = Assembly.GetExecutingAssembly();

        public async Task Write(CardComposeDataDto input, string fileName)
        {
            using (var card = GenerateCardBase(input))
            {
                DrawCardType(card, input);
                DrawCardFrame(card, input);
                await card.WriteAsync(fileName);
            }
        }

        #region [workflow]

        protected MagickImage GenerateCardBase(CardComposeDataDto input)
        {
            return new MagickImage(MagickColors.Transparent, input.Width, input.Height);
        }

        protected void DrawCardType(MagickImage card, CardComposeDataDto input)
        {
            DrawResource(card, input, "card_type", input.Type.ToString("D"));
        }

        protected void DrawCardFrame(MagickImage card, CardComposeDataDto input)
        {
            DrawResource(card, input, input.Rarity, "card-frame");
        }

        #endregion

        #region [helper]

        protected void DrawResource(MagickImage card, CardComposeDataDto input, params string[] names)
        {
            var resourceName = string.Join(".", names);
            resourceName = string.Join(".", "GeneratorCore.Resources", input.Template, resourceName, "png");

            using (var stream = _assembly.GetManifestResourceStream(resourceName))
            using (var layer = new MagickImage(stream))
            {
                card.Composite(layer, Gravity.Center, CompositeOperator.Over);
            }
        }

        #endregion
    }
}
