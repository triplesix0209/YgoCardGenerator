using System.IO;
using System.Threading.Tasks;
using GeneratorCore.Dto;
using ImageMagick;
using TripleSix.Core.Helpers;

namespace GeneratorCore.Services
{
    public class ComposeArtworkService : ComposeService
    {
        public override string Template => "Artwork";

        public override async Task Compose(CardDataDto data, string outputPath, CardSetDto setConfig)
        {
            await base.Compose(data, outputPath, setConfig);

            using (var card = GenerateCard(data, setConfig))
            {
                await DrawArtwork(card, data, setConfig);
                await card.WriteAsync(Path.Combine(outputPath, data.Id + ".png"));
            }
        }

        protected async Task DrawArtwork(MagickImage card, CardDataDto data, CardSetDto setConfig)
        {
            var location = new PointD(0, 0);
            var width = card.Width;
            var height = card.Height;

            if (data.ArtworkPath.IsNullOrWhiteSpace())
            {
                using (var artwork = new MagickImage(MagickColors.White, width, height))
                    card.Composite(artwork, location, CompositeOperator.Over);
            }
            else
            {
                using (var artwork = new MagickImage())
                {
                    await artwork.ReadAsync(data.ArtworkPath);
                    artwork.Resize(new MagickGeometry { Width = width, Height = height, FillArea = true, });
                    artwork.Crop(width, height);

                    card.Composite(artwork, location, CompositeOperator.Over);
                }
            }
        }
    }
}
