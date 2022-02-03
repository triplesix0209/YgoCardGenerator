using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeneratorCore.Dto;
using GeneratorCore.Enums;
using GeneratorCore.Helpers;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using TripleSix.Core.Extensions;
using TripleSix.Core.Helpers;

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
                        DrawMonsterType(context, input);
                        DrawMonsterEffect(context, input);
                        DrawMonsterAtkDef(context, input);
                    }
                });

                await card.SaveAsPngAsync(outputFilename);
            }
        }

        protected void DrawCardType(IImageProcessingContext context, ComposeDataDto input)
        {
            var cardType = input.IsSpellTrap
                ? GetResource("card_type", input.CardType.ToString("D"))
                : GetResource("card_type", input.MonsterPrimaryTypes.First(x => x != MonsterTypes.Pendulum).ToString("D"));

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
            if (input.IsSpellTrap || input.IsMonsterType(MonsterTypes.Xyz, MonsterTypes.Link))
                color = Color.White;
            else
                color = Color.Black;

            context.DrawText(
                input.Name,
                target,
                fontFamily,
                color,
                verticalAlignment: VerticalAlignment.Center,
                anchorPoint: AnchorPositionMode.Left,
                verticalPadding: 0,
                wordwrap: false);
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
            if (input.Attribute.IsNullOrEmpty()) return;
            var attr = input.Attribute.First();
            if (attr == MonsterAttributes.None) return;

            var attribute = GetResource("attribute", attr.ToString("D"));
            context.DrawResource(attribute);
        }

        protected void DrawMonsterLevelRankRating(IImageProcessingContext context, ComposeDataDto input)
        {
            if (input.IsMonsterType(MonsterTypes.Link))
            {
                var linkLabel = GetResource("link_label");
                context.DrawResource(linkLabel);

                var location = new PointF(614, 922);
                var font = "EurostileCandyW01-Semibold";
                var size = 28;
                var color = Color.Black;

                context.DrawText(input.Level.ToString(), location, font, size, color);
                return;
            }

            if (input.Level == 0) return;

            if (input.IsMonsterType(MonsterTypes.Xyz))
            {
                var rank = GetResource("level_rank", $"rnk{input.Level:D}");
                context.DrawResource(rank);
                return;
            }

            var level = GetResource("level_rank", $"lvl{input.Level:D}");
            context.DrawResource(level);
        }

        protected void DrawMonsterType(IImageProcessingContext context, ComposeDataDto input)
        {
            var location = new PointF(53, 766);
            var font = "Yu-Gi-Oh!ITCStoneSerifSmallCaps";
            var size = 22;
            var color = Color.Black;

            var races = input.Race.Select(x => EnumHelper.GetDescription(x));

            var types = new List<string>();
            var primaryTypes = input.MonsterPrimaryTypes.Where(x => new[] { MonsterTypes.Normal, MonsterTypes.Effect }.Contains(x) == false);
            var secondaryTypes = input.MonsterSecondaryTypes.Where(x => new[] { MonsterTypes.Nomi }.Contains(x) == false);
            if (primaryTypes.Any())
                types.AddRange(primaryTypes.Select(x => EnumHelper.GetDescription(x)));
            if (secondaryTypes.Any())
                types.AddRange(secondaryTypes.Select(x => EnumHelper.GetDescription(x)));
            if (input.IsMonsterType(MonsterTypes.Effect))
                types.Add(EnumHelper.GetDescription(MonsterTypes.Effect));

            var text = "["
                + string.Join("/", races)
                + (types.IsNullOrEmpty() ? string.Empty : "/" + string.Join("/", types))
                + "]";
            context.DrawText(text, location, font, size, color);
        }

        protected void DrawMonsterEffect(IImageProcessingContext context, ComposeDataDto input)
        {
            var target = new RectangleF(44, 798, 604, 125);
            var color = Color.Black;

            if (input.IsMonsterType(MonsterTypes.Normal) && input.Flavor.IsNotNullOrWhiteSpace())
            {
                var fontFamily = "Yu-Gi-Oh! StoneSerif LT";
                context.DrawText(input.Flavor, target, fontFamily, color, maxFontSize: 20, verticalPadding: 0);
            }
            else
            {
                var fontFamily = "Yu-Gi-Oh! Matrix Book";
                context.DrawText(input.Effect, target, fontFamily, color, maxFontSize: 20, verticalPadding: 0);
            }
        }

        protected void DrawMonsterAtkDef(IImageProcessingContext context, ComposeDataDto input)
        {
            var level = GetResource("atkdef_line");
            context.DrawResource(level);

            var locationAtk = new PointF(380, 927);
            var locationDef = new PointF(525, 927);
            var font = "MatrixBoldSmallCaps";
            var size = 30;
            var color = Color.Black;

            var atk = "ATK/" + (input.ATK.HasValue ? input.ATK.ToString() : "?").PadLeft(4);
            var def = "DEF/" + (input.DEF.HasValue ? input.DEF.ToString() : "?").PadLeft(4);

            context.DrawText(atk, locationAtk, font, size, color);
            if (input.IsMonsterType(MonsterTypes.Link) == false)
                context.DrawText(def, locationDef, font, size, color);
        }
    }
}
