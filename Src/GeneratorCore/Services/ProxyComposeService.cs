using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using GeneratorCore.Dto;
using GeneratorCore.Enums;
using GeneratorCore.Helpers;
using ImageMagick;
using TripleSix.Core.Helpers;

namespace GeneratorCore.Services
{
    public class ProxyComposeService : BaseComposeService
    {
        public override string Template => "Proxy";

        public override async Task Write(ComposeDataDto input, string outputFilename, CardSetDto setConfig = null)
        {
            await base.Write(input, outputFilename);

            using (var card = GenerateCard(input, setConfig))
            {
                await DrawCardType(card, input, setConfig);
                await DrawArtwork(card, input, setConfig);
                await DrawCardFrame(card, input, setConfig);
                await DrawCardName(card, input, setConfig);
                await DrawLinkArrow(card, input, setConfig);

                if (input.IsSpellTrap)
                {
                    await DrawSpellType(card, input, setConfig);
                    await DrawSpellEffect(card, input, setConfig);
                }
                else
                {
                    await DrawMonsterAttribute(card, input, setConfig);
                    await DrawMonsterLevelRankScaleRating(card, input, setConfig);
                    await DrawMonsterType(card, input, setConfig);
                    await DrawMonsterEffect(card, input, setConfig);
                    await DrawMonsterAtkDef(card, input, setConfig);
                }

                await card.WriteAsync(outputFilename);
            }
        }

        protected PendulumSizes GetPendulumSize(ComposeDataDto input, CardSetDto setConfig)
        {
            var result = input.PendulumSize;
            if (result == PendulumSizes.Auto) result = PendulumSizes.Medium;
            return result;
        }

        protected async Task DrawCardType(MagickImage card, ComposeDataDto input, CardSetDto setConfig)
        {
            await card.DrawResource(input.IsSpellTrap
                ? GetResource("card_type", input.CardType.ToString("D"))
                : GetResource("card_type", input.MonsterPrimaryTypes.First().ToString("D")));

            if (input.IsMonsterType(MonsterTypes.Pendulum))
                await card.DrawResource(GetResource("card_type", MonsterTypes.Pendulum.ToString("D")));
        }

        protected async Task DrawArtwork(MagickImage card, ComposeDataDto input, CardSetDto setConfig)
        {
            var location = new PointD(84, 187);
            var width = 526;
            var height = 526;

            if (input.IsMonsterType(MonsterTypes.Pendulum))
            {
                location = new PointD(44, 182);
                width = 605;
                height = GetPendulumSize(input, setConfig) == PendulumSizes.Large ? 595 : 570;
            }

            if (input.ArtworkPath.IsNullOrWhiteSpace())
            {
                using (var artwork = new MagickImage(MagickColors.White, width, height))
                    card.Composite(artwork, location, CompositeOperator.Over);
            }
            else
            {
                using (var artwork = new MagickImage())
                {
                    await artwork.ReadAsync(input.ArtworkPath);
                    artwork.Resize(new MagickGeometry { Width = width, Height = height, FillArea = true, });
                    artwork.Crop(width, height);

                    card.Composite(artwork, location, CompositeOperator.Over);
                }
            }
        }

        protected async Task DrawCardFrame(MagickImage card, ComposeDataDto input, CardSetDto setConfig)
        {
            string cardFrame;
            if (input.IsMonsterType(MonsterTypes.Pendulum))
            {
                cardFrame = GetResource(
                    "card_frame",
                    input.Rarity.ToString().ToKebabCase(),
                    $"pendulum-{EnumHelper.GetName(GetPendulumSize(input, setConfig)).ToKebabCase()}");
            }
            else
            {
                cardFrame = GetResource(
                    "card_frame",
                    input.Rarity.ToString().ToKebabCase(),
                    "base");
            }

            await card.DrawResource(cardFrame);
        }

        protected async Task DrawCardName(MagickImage card, ComposeDataDto input, CardSetDto setConfig)
        {
            var font = "Yu-Gi-Oh! Matrix Regular Small Caps 1";
            var size = 80;
            var location = new Point(50, 43);
            var maxWidth = 525;
            var color = input.IsSpellTrap || input.IsMonsterType(MonsterTypes.Xyz, MonsterTypes.Link)
                ? MagickColors.White
                : MagickColors.Black;

            await card.DrawTextLine(input.Name, location, font, size, color, maxWidth);
        }

        protected async Task DrawLinkArrow(MagickImage card, ComposeDataDto input, CardSetDto setConfig)
        {
            if (!input.IsLink) return;

            foreach (var arrow in EnumHelper.GetValues<LinkArrows>())
            {
                var arrowName = arrow.ToString("D");
                if (input.IsMonsterType(MonsterTypes.Pendulum)) arrowName += "-pendulum";
                arrowName += input.HasLinkArrow(arrow) ? "-on" : "-off";

                await card.DrawResource(GetResource("link_arrow", input.Rarity.ToString().ToKebabCase(), arrowName));
            }
        }

        protected async Task DrawSpellType(MagickImage card, ComposeDataDto input, CardSetDto setConfig)
        {
            if (input.SpellType == SpellTypes.Link)
                return;

            if (input.SpellType == SpellTypes.Normal)
            {
                var spellTypeText = GetResource("spelltrap_type", $"{input.CardType:D}_0");
                await card.DrawResource(spellTypeText);
            }
            else
            {
                var spellTypeText = GetResource("spelltrap_type", $"{input.CardType:D}_1");
                var spellTypeIcon = GetResource("spelltrap_type", $"{input.SpellType:D}");
                await card.DrawResource(spellTypeText);
                await card.DrawResource(spellTypeIcon);
            }
        }

        protected async Task DrawSpellEffect(MagickImage card, ComposeDataDto input, CardSetDto setConfig)
        {
            if (input.Effect.IsNullOrWhiteSpace()) return;

            var target = new Rectangle(55, 765, 580, 185);
            var font = "Yu-Gi-Oh! Matrix Book";

            await card.DrawTextAreaAsync(input.Effect, target, font);
        }

        protected async Task DrawMonsterAttribute(MagickImage card, ComposeDataDto input, CardSetDto setConfig)
        {
            if (input.Attribute.IsNullOrEmpty()) return;
            var attr = input.Attribute.First();
            if (attr == MonsterAttributes.None) return;

            var attribute = GetResource("attribute", attr.ToString("D"));

            await card.DrawResource(attribute);
        }

        protected async Task DrawMonsterLevelRankScaleRating(MagickImage card, ComposeDataDto input, CardSetDto setConfig)
        {
            if (input.IsMonsterType(MonsterTypes.Pendulum))
            {
                var leftScaleLocation = new Point(55, 690);
                var rightScaleLocation = new Point(610, 690);

                switch (GetPendulumSize(input, setConfig))
                {
                    case PendulumSizes.Small:
                    case PendulumSizes.Large:
                        leftScaleLocation = new Point(55, 705);
                        rightScaleLocation = new Point(610, 705);
                        break;
                }

                var font = "Yu-Gi-Oh! Matrix Regular Small Caps 1";
                var size = 54;
                var width = 30;
                var leftScale = input.LeftScale.HasValue ? input.LeftScale.ToString() : "?";
                var rightScale = input.RightScale.HasValue ? input.RightScale.ToString() : "?";

                await card.DrawTextLine(leftScale, leftScaleLocation, font, size, width: width, gravity: Gravity.North);
                await card.DrawTextLine(rightScale, rightScaleLocation, font, size, width: width, gravity: Gravity.North);
            }

            if (input.IsMonsterType(MonsterTypes.Link))
            {
                var location = new Point(615, 923);
                var font = "Eurostile Candy W01 Semibold";
                var size = 28;
                var linkRating = input.LinkRating.HasValue ? input.LinkRating.ToString() : "0";

                await card.DrawResource(GetResource("link_label"));
                await card.DrawTextLine(linkRating, location, font, size);
                return;
            }

            if (input.IsMonsterType(MonsterTypes.Xyz))
            {
                if (input.Rank is null || input.Rank == 0) return;

                var rank = GetResource("level_rank", $"rnk{input.Rank:D}");
                await card.DrawResource(rank);
                return;
            }

            if (input.Level.HasValue && input.Level > 0)
            {
                var level = GetResource("level_rank", $"lvl{input.Level:D}");
                await card.DrawResource(level);
            }
        }

        protected async Task DrawMonsterType(MagickImage card, ComposeDataDto input, CardSetDto setConfig)
        {
            var location = new Point(53, 760);
            if (input.IsMonsterType(MonsterTypes.Pendulum) && GetPendulumSize(input, setConfig) == PendulumSizes.Large)
                location = new Point(53, 785);

            var font = "Yu-Gi-Oh! ITC Stone Serif Small Caps Bold";
            var size = 24;
            var maxWidth = 590;

            var races = input.Race.Select(x => EnumHelper.GetDescription(x));
            var types = new List<string>();
            var primaryTypes = input.MonsterPrimaryTypes.Where(x => new[] { MonsterTypes.Normal, MonsterTypes.Effect }.Contains(x) == false);
            var secondaryTypes = input.MonsterSecondaryTypes.Where(x => new[] { MonsterTypes.Pendulum, MonsterTypes.Nomi }.Contains(x) == false);

            if (primaryTypes.Any())
                types.AddRange(primaryTypes.Select(x => EnumHelper.GetDescription(x)));
            if (input.IsMonsterType(MonsterTypes.Pendulum))
                types.Add(EnumHelper.GetDescription(MonsterTypes.Pendulum));
            if (secondaryTypes.Any())
                types.AddRange(secondaryTypes.Select(x => EnumHelper.GetDescription(x)));
            if (!input.IsMonsterType(MonsterTypes.Token))
            {
                if (input.IsMonsterType(MonsterTypes.Effect))
                    types.Add(EnumHelper.GetDescription(MonsterTypes.Effect));
                else if (input.IsMonsterType(MonsterTypes.Normal))
                    types.Add(EnumHelper.GetDescription(MonsterTypes.Normal));
            }

            var text = "["
                + string.Join("/", races)
                + (types.IsNullOrEmpty() ? string.Empty : "/" + string.Join("/", types))
                + "]";

            await card.DrawTextLine(text, location, font, size, maxWidth: maxWidth);
        }

        protected async Task DrawMonsterEffect(MagickImage card, ComposeDataDto input, CardSetDto setConfig)
        {
            Rectangle target;

            if (input.IsMonsterType(MonsterTypes.Pendulum) && input.PendulumEffect.IsNotNullOrWhiteSpace())
            {
                switch (GetPendulumSize(input, setConfig))
                {
                    case PendulumSizes.Large:
                        target = new Rectangle(108, 640, 478, 135);
                        break;

                    case PendulumSizes.Small:
                        target = new Rectangle(108, 675, 478, 75);
                        break;

                    default:
                        target = new Rectangle(108, 640, 478, 110);
                        break;
                }

                var font = "Yu-Gi-Oh! Matrix Book";
                await card.DrawTextAreaAsync(input.PendulumEffect, target, font);
            }

            var cardText = input.Effect.IsNotNullOrWhiteSpace() ? input.Effect : input.Flavor;
            if (cardText.IsNotNullOrWhiteSpace())
            {
                target = new Rectangle(55, 790, 580, 130);
                if (input.IsMonsterType(MonsterTypes.Pendulum) && GetPendulumSize(input, setConfig) == PendulumSizes.Large)
                    target = new Rectangle(55, 815, 580, 105);

                var font = (input.IsMonsterType(MonsterTypes.Normal) && input.Flavor.IsNotNullOrWhiteSpace())
                        ? "Yu-Gi-Oh! ITC Stone Serif LT Italic"
                        : "Yu-Gi-Oh! Matrix Book";
                await card.DrawTextAreaAsync(cardText, target, font);
            }
        }

        protected async Task DrawMonsterAtkDef(MagickImage card, ComposeDataDto input, CardSetDto setConfig)
        {
            var locationAtk = new Point(380, 927);
            var locationDef = new Point(525, 927);
            var font = "Matrix Bold Small Caps";
            var size = 30;
            var atk = "ATK/" + (input.ATK.HasValue ? input.ATK.ToString() : "?").PadLeft(4);
            var def = "DEF/" + (input.DEF.HasValue ? input.DEF.ToString() : "?").PadLeft(4);

            await card.DrawResource(GetResource("atkdef_line"));
            await card.DrawTextLine(atk, locationAtk, font, size);
            if (input.IsMonsterType(MonsterTypes.Link) == false)
                await card.DrawTextLine(def, locationDef, font, size);
        }
    }
}
