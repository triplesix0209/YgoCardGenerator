using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GeneratorCore.Dto;
using GeneratorCore.Enums;
using GeneratorCore.Helpers;
using ImageMagick;
using TripleSix.Core.Helpers;

namespace GeneratorCore.Services
{
    public class ComposeProxyService : ComposeService
    {
        public override string Template => "Proxy";

        public override async Task Compose(CardDataDto data, string outputPath, CardSetDto setConfig)
        {
            await base.Compose(data, outputPath, setConfig);

            using (var card = GenerateCard(data, setConfig))
            {
                await DrawCardType(card, data, setConfig);
                await DrawArtwork(card, data, setConfig);
                await DrawCardFrame(card, data, setConfig);
                await DrawCardName(card, data, setConfig);
                await DrawLinkArrow(card, data, setConfig);

                if (data.IsSpellTrap)
                {
                    await DrawSpellType(card, data, setConfig);
                    await DrawSpellEffect(card, data, setConfig);
                }
                else
                {
                    await DrawMonsterAttribute(card, data, setConfig);
                    await DrawMonsterLevelRankScaleRating(card, data, setConfig);
                    await DrawMonsterType(card, data, setConfig);
                    await DrawMonsterEffect(card, data, setConfig);
                    await DrawMonsterAtkDef(card, data, setConfig);
                }

                await card.WriteAsync(Path.Combine(outputPath, data.Id + ".png"));

                if (setConfig.DrawField && data.IsSpellTrap && data.IsSpellType(SpellTypes.Field))
                {
                    var fieldPath = Path.Combine(outputPath, "field");
                    if (!Directory.Exists(fieldPath))
                        Directory.CreateDirectory(fieldPath);

                    using (var artwork = new MagickImage())
                    {
                        await artwork.ReadAsync(data.ArtworkPath);
                        artwork.Resize(new MagickGeometry { Width = 512, Height = 512, FillArea = true, });
                        artwork.Crop(512, 512);

                        var field = new MagickImage(MagickColors.Transparent, 512, 512);
                        field.Composite(artwork, new PointD(0, 0), CompositeOperator.Over);

                        await field.WriteAsync(Path.Combine(fieldPath, data.Id + ".png"));
                    }
                }
            }
        }

        protected PendulumSizes GetPendulumSize(CardDataDto data, CardSetDto setConfig)
        {
            var result = data.PendulumSize;
            if (result == PendulumSizes.Auto) result = PendulumSizes.Medium;
            return result;
        }

        protected async Task DrawCardType(MagickImage card, CardDataDto data, CardSetDto setConfig)
        {
            await card.DrawResource(data.IsSpellTrap
                ? GetResource("card_type", data.CardType.ToString("D"))
                : GetResource("card_type", data.MonsterPrimaryTypes.First().ToString("D")));

            if (data.IsMonsterType(MonsterTypes.Pendulum))
                await card.DrawResource(GetResource("card_type", MonsterTypes.Pendulum.ToString("D")));
        }

        protected async Task DrawArtwork(MagickImage card, CardDataDto data, CardSetDto setConfig)
        {
            var location = new PointD(84, 187);
            var width = 526;
            var height = 526;

            if (data.IsMonsterType(MonsterTypes.Pendulum))
            {
                location = new PointD(44, 182);
                width = 605;
                height = GetPendulumSize(data, setConfig) == PendulumSizes.Large ? 595 : 570;
            }

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

        protected async Task DrawCardFrame(MagickImage card, CardDataDto data, CardSetDto setConfig)
        {
            string cardFrame;
            if (data.IsMonsterType(MonsterTypes.Pendulum))
            {
                cardFrame = GetResource(
                    "card_frame",
                    data.Rarity.ToString().ToKebabCase(),
                    $"pendulum-{EnumHelper.GetName(GetPendulumSize(data, setConfig)).ToKebabCase()}");
            }
            else
            {
                cardFrame = GetResource(
                    "card_frame",
                    data.Rarity.ToString().ToKebabCase(),
                    "base");
            }

            await card.DrawResource(cardFrame);
        }

        protected async Task DrawCardName(MagickImage card, CardDataDto data, CardSetDto setConfig)
        {
            var font = "Yu-Gi-Oh! Matrix Regular Small Caps 1";
            var size = 80;
            var location = new Point(50, 43);
            var maxWidth = 525;
            var color = data.IsSpellTrap || data.IsMonsterType(MonsterTypes.Xyz, MonsterTypes.Link)
                ? MagickColors.White
                : MagickColors.Black;
            if (data.Rarity == CardRarities.Gold)
                color = MagickColors.Gold;

            await card.DrawTextLine(data.Name, location, font, size, color, maxWidth: maxWidth);
        }

        protected async Task DrawLinkArrow(MagickImage card, CardDataDto data, CardSetDto setConfig)
        {
            if (!data.IsLink) return;

            foreach (var arrow in EnumHelper.GetValues<LinkArrows>())
            {
                var arrowName = arrow.ToString("D");
                if (data.IsMonsterType(MonsterTypes.Pendulum)) arrowName += "-pendulum";
                arrowName += data.HasLinkArrow(arrow) ? "-on" : "-off";

                await card.DrawResource(GetResource("link_arrow", data.Rarity.ToString().ToKebabCase(), arrowName));
            }
        }

        protected async Task DrawSpellType(MagickImage card, CardDataDto data, CardSetDto setConfig)
        {
            if (data.SpellType == SpellTypes.Link)
                return;

            if (data.SpellType == SpellTypes.Normal)
            {
                var spellTypeText = GetResource("spelltrap_type", $"{data.CardType:D}_0");
                await card.DrawResource(spellTypeText);
            }
            else
            {
                var spellTypeText = GetResource("spelltrap_type", $"{data.CardType:D}_1");
                var spellTypeIcon = GetResource("spelltrap_type", $"{data.SpellType:D}");
                await card.DrawResource(spellTypeText);
                await card.DrawResource(spellTypeIcon);
            }
        }

        protected async Task DrawSpellEffect(MagickImage card, CardDataDto data, CardSetDto setConfig)
        {
            if (data.Effect.IsNullOrWhiteSpace()) return;

            var target = new Rectangle(55, 765, 580, 185);
            var font = "Yu-Gi-Oh! Matrix Book";

            await card.DrawTextAreaAsync(data.Effect, target, font);
        }

        protected async Task DrawMonsterAttribute(MagickImage card, CardDataDto data, CardSetDto setConfig)
        {
            if (data.Attribute.IsNullOrEmpty()) return;
            var attr = data.Attribute.First();
            if (attr == MonsterAttributes.None) return;

            var attribute = GetResource("attribute", attr.ToString("D"));

            await card.DrawResource(attribute);
        }

        protected async Task DrawMonsterLevelRankScaleRating(MagickImage card, CardDataDto data, CardSetDto setConfig)
        {
            if (data.IsMonsterType(MonsterTypes.Pendulum))
            {
                var leftScaleLocation = new Point(55, 690);
                var rightScaleLocation = new Point(610, 690);

                switch (GetPendulumSize(data, setConfig))
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
                var leftScale = data.LeftScale.HasValue ? data.LeftScale.ToString() : "?";
                var rightScale = data.RightScale.HasValue ? data.RightScale.ToString() : "?";

                await card.DrawTextLine(leftScale, leftScaleLocation, font, size, width: width, gravity: Gravity.North);
                await card.DrawTextLine(rightScale, rightScaleLocation, font, size, width: width, gravity: Gravity.North);
            }

            if (data.IsMonsterType(MonsterTypes.Link))
            {
                var location = new Point(615, 923);
                var font = "Eurostile Candy W01 Semibold";
                var size = 28;
                var linkRating = data.LinkRating.HasValue ? data.LinkRating.ToString() : "0";

                await card.DrawResource(GetResource("link_label"));
                await card.DrawTextLine(linkRating, location, font, size);
                return;
            }

            if (data.IsMonsterType(MonsterTypes.Xyz))
            {
                if (data.Rank is null || data.Rank == 0) return;

                var rank = GetResource("level_rank", $"rnk{data.Rank:D}");
                await card.DrawResource(rank);
                return;
            }

            if (data.Level.HasValue && data.Level > 0)
            {
                var level = GetResource("level_rank", $"lvl{data.Level:D}");
                await card.DrawResource(level);
            }
        }

        protected async Task DrawMonsterType(MagickImage card, CardDataDto data, CardSetDto setConfig)
        {
            var location = new Point(53, 760);
            if (data.IsMonsterType(MonsterTypes.Pendulum) && GetPendulumSize(data, setConfig) == PendulumSizes.Large)
                location = new Point(53, 785);

            var font = "Yu-Gi-Oh! ITC Stone Serif Small Caps Bold";
            var size = 24;
            var maxWidth = 590;

            var races = data.Race.Select(x => EnumHelper.GetDescription(x));
            var types = new List<string>();
            var primaryTypes = data.MonsterPrimaryTypes.Where(x => new[] { MonsterTypes.Normal, MonsterTypes.Effect }.Contains(x) == false);
            var secondaryTypes = data.MonsterSecondaryTypes.Where(x => new[] { MonsterTypes.Pendulum, MonsterTypes.Nomi }.Contains(x) == false);

            if (primaryTypes.Any())
                types.AddRange(primaryTypes.Select(x => EnumHelper.GetDescription(x)));
            if (data.IsMonsterType(MonsterTypes.Pendulum))
                types.Add(EnumHelper.GetDescription(MonsterTypes.Pendulum));
            if (secondaryTypes.Any())
                types.AddRange(secondaryTypes.Select(x => EnumHelper.GetDescription(x)));
            if (!data.IsMonsterType(MonsterTypes.Token))
            {
                if (data.IsMonsterType(MonsterTypes.Effect))
                    types.Add(EnumHelper.GetDescription(MonsterTypes.Effect));
                else if (data.IsMonsterType(MonsterTypes.Normal))
                    types.Add(EnumHelper.GetDescription(MonsterTypes.Normal));
            }

            var text = "["
                + string.Join("/", races)
                + (types.IsNullOrEmpty() ? string.Empty : "/" + string.Join("/", types))
                + "]";

            await card.DrawTextLine(text, location, font, size, maxWidth: maxWidth);
        }

        protected async Task DrawMonsterEffect(MagickImage card, CardDataDto data, CardSetDto setConfig)
        {
            Rectangle target;

            if (data.IsMonsterType(MonsterTypes.Pendulum) && data.PendulumEffect.IsNotNullOrWhiteSpace())
            {
                switch (GetPendulumSize(data, setConfig))
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
                await card.DrawTextAreaAsync(data.PendulumEffect, target, font);
            }

            var cardText = data.Effect.IsNotNullOrWhiteSpace() ? data.Effect : data.Flavor;
            if (cardText.IsNotNullOrWhiteSpace())
            {
                target = new Rectangle(55, 790, 580, 130);
                if (data.IsMonsterType(MonsterTypes.Pendulum) && GetPendulumSize(data, setConfig) == PendulumSizes.Large)
                    target = new Rectangle(55, 815, 580, 105);

                var font = (data.IsMonsterType(MonsterTypes.Normal) && data.Flavor.IsNotNullOrWhiteSpace())
                        ? "Yu-Gi-Oh! ITC Stone Serif LT Italic"
                        : "Yu-Gi-Oh! Matrix Book";
                await card.DrawTextAreaAsync(cardText, target, font);
            }
        }

        protected async Task DrawMonsterAtkDef(MagickImage card, CardDataDto data, CardSetDto setConfig)
        {
            var locationAtk = new Point(380, 927);
            var locationDef = new Point(525, 927);
            var font = "Matrix Bold Small Caps";
            var size = 30;
            var atk = "ATK/" + (data.Atk.HasValue ? data.Atk.ToString() : "?").PadLeft(4);
            var def = "DEF/" + (data.Def.HasValue ? data.Def.ToString() : "?").PadLeft(4);

            await card.DrawResource(GetResource("atkdef_line"));
            await card.DrawTextLine(atk, locationAtk, font, size);
            if (data.IsMonsterType(MonsterTypes.Link) == false)
                await card.DrawTextLine(def, locationDef, font, size);
        }
    }
}
