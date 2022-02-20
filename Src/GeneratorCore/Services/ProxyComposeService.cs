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

        public override async Task Write(ComposeDataDto input, string outputFilename)
        {
            await base.Write(input, outputFilename);

            using (var card = GenerateCard(input))
            {
                DrawCardType(card, input);
                DrawCardFrame(card, input);
                DrawCardName(card, input);
                DrawArtwork(card, input);

                if (input.IsSpellTrap)
                {
                    DrawSpellType(card, input);
                    DrawSpellEffect(card, input);
                }
                else
                {
                    DrawMonsterAttribute(card, input);
                    DrawMonsterLevelRankRating(card, input);
                    DrawMonsterType(card, input);
                    DrawMonsterEffect(card, input);
                    DrawMonsterAtkDef(card, input);
                }

                await card.WriteAsync(outputFilename);
            }
        }

        protected void DrawCardType(MagickImage card, ComposeDataDto input)
        {
            var cardType = input.IsSpellTrap
                ? GetResource("card_type", input.CardType.ToString("D"))
                : GetResource("card_type", input.MonsterPrimaryTypes.First(x => x != MonsterTypes.Pendulum).ToString("D"));

            card.DrawResource(cardType);
        }

        protected void DrawCardFrame(MagickImage card, ComposeDataDto input)
        {
            var cardFrame = GetResource("card_frame", input.Rarity.ToString().ToKebabCase(), "card-frame");
            card.DrawResource(cardFrame);
        }

        protected void DrawCardName(MagickImage card, ComposeDataDto input)
        {
            var font = "Yu-Gi-Oh! Matrix Regular Small Caps 1";
            var size = 80;
            var location = new PointD(50, 43);
            var maxWith = 525;
            var color = input.IsSpellTrap || input.IsMonsterType(MonsterTypes.Xyz, MonsterTypes.Link)
                ? MagickColors.White
                : MagickColors.Black;

            card.DrawTextLine(input.Name, location, font, color, size, maxWidth: maxWith);
        }

        protected void DrawArtwork(MagickImage card, ComposeDataDto input)
        {
            if (input.ArtworkPath.IsNullOrWhiteSpace()) return;

            using (var artwork = new MagickImage(input.ArtworkPath))
            {
                artwork.Resize(525, 525);

                var location = new PointD(84, 187);
                card.Composite(artwork, location, CompositeOperator.Over);
            }
        }

        protected void DrawSpellType(MagickImage card, ComposeDataDto input)
        {
            if (input.SpellType == SpellTypes.Normal)
            {
                var spellTypeText = GetResource("spelltrap_type", $"{input.CardType:D}_0");
                card.DrawResource(spellTypeText);
            }
            else
            {
                var spellTypeText = GetResource("spelltrap_type", $"{input.CardType:D}_1");
                var spellTypeIcon = GetResource("spelltrap_type", $"{input.SpellType:D}");
                card.DrawResource(spellTypeText)
                    .DrawResource(spellTypeIcon);
            }
        }

        protected void DrawSpellEffect(MagickImage card, ComposeDataDto input)
        {
            var target = new Rectangle(55, 765, 580, 180);
            var font = "Yu-Gi-Oh! Matrix Book";
            var color = MagickColors.Black;
            var maxFontSize = 20;

            card.DrawTextArea(input.Effect, target, font, color, maxFontSize: maxFontSize, gravity: Gravity.Northwest);
        }

        protected void DrawMonsterAttribute(MagickImage card, ComposeDataDto input)
        {
            if (input.Attribute.IsNullOrEmpty()) return;
            var attr = input.Attribute.First();
            if (attr == MonsterAttributes.None) return;

            var attribute = GetResource("attribute", attr.ToString("D"));
            card.DrawResource(attribute);
        }

        protected void DrawMonsterLevelRankRating(MagickImage card, ComposeDataDto input)
        {
            if (input.IsMonsterType(MonsterTypes.Link))
            {
                var linkLabel = GetResource("link_label");
                card.DrawResource(linkLabel);

                var location = new PointD(615, 923);
                var font = "Eurostile Candy W01 Semibold";
                var size = 28;
                var color = MagickColors.Black;

                card.DrawTextLine(input.Level.ToString(), location, font, color, size);
                return;
            }

            if (input.Level == 0) return;

            if (input.IsMonsterType(MonsterTypes.Xyz))
            {
                var rank = GetResource("level_rank", $"rnk{input.Level:D}");
                card.DrawResource(rank);
                return;
            }

            var level = GetResource("level_rank", $"lvl{input.Level:D}");
            card.DrawResource(level);
        }

        protected void DrawMonsterType(MagickImage card, ComposeDataDto input)
        {
            var location = new PointD(53, 763);
            var font = "Yu-Gi-Oh! ITC Stone Serif Small Caps Bold";
            var size = 24;
            var maxWidth = 590;
            var color = MagickColors.Black;

            var races = input.Race.Select(x => EnumHelper.GetDescription(x));

            var types = new List<string>();
            var primaryTypes = input.MonsterPrimaryTypes.Where(x => new[] { MonsterTypes.Normal, MonsterTypes.Effect }.Contains(x) == false);
            var secondaryTypes = input.MonsterSecondaryTypes.Where(x => new[] { MonsterTypes.Nomi }.Contains(x) == false);
            if (primaryTypes.Any())
                types.AddRange(primaryTypes.Select(x => EnumHelper.GetDescription(x)));
            if (input.IsMonsterType(MonsterTypes.Pendulum))
                types.Add(EnumHelper.GetDescription(MonsterTypes.Pendulum));
            if (secondaryTypes.Any())
                types.AddRange(secondaryTypes.Select(x => EnumHelper.GetDescription(x)));
            if (input.IsMonsterType(MonsterTypes.Effect))
                types.Add(EnumHelper.GetDescription(MonsterTypes.Effect));
            else if (input.IsMonsterType(MonsterTypes.Normal))
                types.Add(EnumHelper.GetDescription(MonsterTypes.Normal));

            var text = "["
                + string.Join("/", races)
                + (types.IsNullOrEmpty() ? string.Empty : "/" + string.Join("/", types))
                + "]";

            card.DrawTextLine(text, location, font, color, size, maxWidth: maxWidth);
        }

        protected void DrawMonsterEffect(MagickImage card, ComposeDataDto input)
        {
            var target = new Rectangle(55, 795, 580, 125);
            var font = (input.IsMonsterType(MonsterTypes.Normal) && input.Flavor.IsNotNullOrWhiteSpace())
                    ? "Yu-Gi-Oh! ITC Stone Serif LT Italic"
                    : "Yu-Gi-Oh! Matrix Book";
            var color = MagickColors.Black;
            var maxFontSize = 20;
            var text = input.Effect.IsNotNullOrWhiteSpace() ? input.Effect : input.Flavor;

            card.DrawTextArea(text, target, font, color, maxFontSize: maxFontSize, gravity: Gravity.Northwest);
        }

        protected void DrawMonsterAtkDef(MagickImage card, ComposeDataDto input)
        {
            card.DrawResource(GetResource("atkdef_line"));

            var locationAtk = new PointD(380, 927);
            var locationDef = new PointD(530, 927);
            var font = "Matrix Bold Small Caps";
            var size = 30;
            var color = MagickColors.Black;

            var atk = "ATK/" + (input.ATK.HasValue ? input.ATK.ToString() : "?").PadLeft(4);
            var def = "DEF/" + (input.DEF.HasValue ? input.DEF.ToString() : "?").PadLeft(4);

            card.DrawTextLine(atk, locationAtk, font, color, size);
            if (input.IsMonsterType(MonsterTypes.Link) == false)
                card.DrawTextLine(def, locationDef, font, color, size);
        }
    }
}
