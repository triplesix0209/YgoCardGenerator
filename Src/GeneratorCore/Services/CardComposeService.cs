using System.Threading.Tasks;
using ImageMagick;
using TripleSix.Core.Services;

namespace GeneratorCore.Services
{
    public class CardComposeService : BaseService,
        ICardComposeService
    {
        private const int _cardWidth = 694;
        private const int _cardHeigth = 1013;

        public async Task Write(string fileName)
        {
            using (var card = GenerateCardBase())
            {
                new Drawables()
                  .FontPointSize(72)
                  .Font("Comic Sans")
                  .StrokeColor(new MagickColor("yellow"))
                  .FillColor(MagickColors.Orange)
                  .TextAlignment(TextAlignment.Center)
                  .Text(256, 64, "Magick.NET")
                  .StrokeColor(new MagickColor(0, Quantum.Max, 0))
                  .FillColor(MagickColors.SaddleBrown)
                  .Ellipse(256, 96, 192, 8, 0, 360)
                  .Draw(card);

                await card.WriteAsync(fileName);
            }
        }

        protected MagickImage GenerateCardBase()
        {
            return new MagickImage(MagickColors.Transparent, _cardWidth, _cardHeigth);
        }
    }
}
