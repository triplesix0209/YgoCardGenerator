using System;
using System.Threading.Tasks;
using GeneratorCore.Dto;
using GeneratorCore.Enums;
using GeneratorCore.Helpers;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using TripleSix.Core.Extensions;

namespace GeneratorCore.Services
{
    public class ProxyComposeService : BaseComposeService
    {
        public override string Template => "Proxy";

        public override async Task Write(ComposeDataDto input, string outputFilename)
        {
            await base.Write(input, outputFilename);
            using (var card = GenerateCard(input))
            {
                card.Mutate(context =>
                {
                    DrawCardType(context, input);
                    DrawCardFrame(context, input);
                    DrawCardName(context, input);
                    DrawArtwork(context, input);

                    if (input.IsSpellTrap)
                    {
                        DrawSpellType(context, input);
                        DrawSpellEffect(context, input);
                    }
                    else
                    {
                        DrawMonsterAttribute(context, input);
                        DrawMonsterLevelRankRating(context, input);
                        DrawMonsterAtkDef(context, input);
                    }
                });

                await card.SaveAsPngAsync(outputFilename);
            }
        }

        protected void DrawCardType(IImageProcessingContext context, ComposeDataDto input)
        {
            var cardType = GetResource("card_type", input.CardType.ToString("D"));
            context.DrawResource(cardType);
        }

        protected void DrawCardFrame(IImageProcessingContext context, ComposeDataDto input)
        {
            var cardFrame = GetResource("card_frame", input.Rarity.ToString().ToKebabCase(), "card-frame");
            context.DrawResource(cardFrame);
        }

        protected void DrawCardName(IImageProcessingContext context, ComposeDataDto input)
        {
            var target = new RectangleF(42, 50, 540, 65);
            var fontFamily = "Yu-Gi-Oh! Matrix Small Caps 1";

            Color color;
            switch (input.CardType)
            {
                case CardTypes.Spell:
                case CardTypes.Trap:
                case CardTypes.Xyz:
                case CardTypes.Link:
                    color = Color.White;
                    break;

                default:
                    color = Color.Black;
                    break;
            }

            context.DrawText(
                input.Name,
                target,
                fontFamily,
                color,
                wordwrap: false,
                horizontalAlignment: HorizontalAlignment.Left,
                verticalAlignment: VerticalAlignment.Center,
                anchorPoint: AnchorPositionMode.Left,
                verticalPadding: 0);
        }

        protected void DrawArtwork(IImageProcessingContext context, ComposeDataDto input)
        {
            if (input.ArtworkPath.IsNullOrWhiteSpace()) return;

            using (var artwork = Image.Load(input.ArtworkPath))
            {
                var location = new Point(84, 187);
                artwork.Mutate(c => c.Resize(526, 526));
                context.DrawImage(artwork, location, 1);
            }
        }

        protected void DrawSpellType(IImageProcessingContext context, ComposeDataDto input)
        {
            if (input.SpellType == SpellTypes.Normal)
            {
                var spellTypeText = GetResource("spelltrap_type", $"{input.CardType:D}_0");
                context.DrawResource(spellTypeText);
            }
            else
            {
                var spellTypeText = GetResource("spelltrap_type", $"{input.CardType:D}_1");
                var spellTypeIcon = GetResource("spelltrap_type", $"{input.SpellType:D}");
                context.DrawResource(spellTypeText)
                    .DrawResource(spellTypeIcon);
            }
        }

        protected void DrawSpellEffect(IImageProcessingContext context, ComposeDataDto input)
        {
            var target = new RectangleF(44, 758, 604, 197);
            var fontFamily = "Yu-Gi-Oh! Matrix Book";
            var color = Color.Black;

            context.DrawText(input.Effect, target, fontFamily, color, maxFontSize: 20);
        }

        protected void DrawMonsterAttribute(IImageProcessingContext context, ComposeDataDto input)
        {
            if (input.Attribute == MonsterAttributes.None) return;

            var attribute = GetResource("attribute", input.Attribute.ToString("D"));
            context.DrawResource(attribute);
        }

        protected void DrawMonsterLevelRankRating(IImageProcessingContext context, ComposeDataDto input)
        {
            if (input.CardType == CardTypes.Link)
            {
                var linkLabel = GetResource("link_label");
                context.DrawResource(linkLabel);

                var target = new RectangleF(614, 925, 27, 25);
                var font = "EurostileCandyW01-Semibold";
                var color = Color.Black;

                context.DrawText(
                    input.Level.ToString(),
                    target,
                    font,
                    color,
                    wordwrap: false,
                    anchorPoint: AnchorPositionMode.Center,
                    horizontalAlignment: HorizontalAlignment.Center,
                    verticalAlignment: VerticalAlignment.Center,
                    horizontalPadding: 0,
                    verticalPadding: 0);
                return;
            }

            if (input.Level == 0) return;

            if (input.CardType == CardTypes.Xyz)
            {
                var rank = GetResource("level_rank", $"rnk{input.Level:D}");
                context.DrawResource(rank);
                return;
            }

            var level = GetResource("level_rank", $"lvl{input.Level:D}");
            context.DrawResource(level);
        }

        protected void DrawMonsterAtkDef(IImageProcessingContext context, ComposeDataDto input)
        {
            var level = GetResource("atkdef_line");
            context.DrawResource(level);

            var atk = "ATK/" + (input.ATK.HasValue ? input.ATK : "?");
            var def = "DEF/" + (input.DEF.HasValue ? input.DEF : "?");
            var targetAtk = new RectangleF(371, 927, 127, 30);
            var targetDef = new RectangleF(515, 927, 127, 30);
            var font = "MatrixBoldSmallCaps";
            var color = Color.Black;

            context.DrawText(
                atk,
                targetAtk,
                font,
                color,
                wordwrap: false,
                horizontalPadding: 0,
                verticalPadding: 0);

            if (input.CardType != CardTypes.Link)
            {
                context.DrawText(
                def,
                targetDef,
                font,
                color,
                wordwrap: false,
                horizontalPadding: 0,
                verticalPadding: 0);
            }
        }
    }
}
