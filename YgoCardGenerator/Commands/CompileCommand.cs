using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System.Reflection;
using Topten.RichTextKit;
using YgoCardGenerator.Persistences;
using YgoCardGenerator.Persistences.Entities;

namespace YgoCardGenerator.Commands
{
    public class CompileCommand : AppCommand
    {
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        private static readonly Type textEntityType = typeof(Text);

        public CompileCommand(string[] args, ILogger logger)
            : base(args, logger)
        {
        }

        protected override CommandArgument[] ArgumentSchema => new CommandArgument[]
        {
            new ("set") { Description = "path card set filename to compile (TOML)" },
            new ("max-thread") { Description = "number of thread use to compile", DefaultValue = "10" },
        };

        public override async Task Do()
        {
            #region [load data]

            // read argument
            var cardSetFilename = Arguments[0].Value();
            var maxThread = int.Parse(Arguments[1].Value()!);

            // cardset
            var cardSet = Toml.ToModel<CardSetDto>(await File.ReadAllTextAsync(cardSetFilename!));
            cardSet.BasePath = Path.GetDirectoryName(cardSetFilename)!;
            cardSet.ValidateAndThrow();
            var config = new CardSetConfig(cardSet);
            if (cardSet.BasePath == null || cardSet.CardDbPath == null || cardSet.Packs == null) return;

            // utility
            var utilities = new Dictionary<string, string>();
            foreach (var filePath in Directory.GetFiles(config.UtilityDirectory))
                utilities.Add(Path.GetFileName(filePath), filePath);
            foreach (var packPath in cardSet.Packs)
            {
                var utilityPath = Path.Combine(config.BasePath, packPath, "script/utility");
                if (!Directory.Exists(utilityPath)) continue;

                foreach (var filePath in Directory.GetFiles(utilityPath))
                {
                    var fileName = Path.GetFileName(filePath);
                    if (utilities.ContainsKey(fileName)) throw new Exception($"duplicate utility {fileName} in {packPath}");
                    utilities.Add(fileName, filePath);
                }
            }

            // card
            var cardIds = new List<int>();
            var cards = new Queue<CardDataDto>();
            foreach (var packPath in cardSet.Packs)
            {
                var cardPackPath = Path.Combine(cardSet.BasePath, packPath);
                var listCard = Toml.ToModel(await File.ReadAllTextAsync(Path.Combine(cardPackPath, CardSetConfig.CardIndexFileName)));

                foreach (var card in listCard.Values)
                {
                    var cardInputOptions = new TomlModelOptions { ConvertPropertyName = (name) => name.ToKebabCase() };
                    var cardInput = Toml.ToModel<CardInputDto>(Toml.FromModel(card), options: cardInputOptions);
                    var cardData = cardInput.ToCardDataDto(cardPackPath);
                    cardData.ValidateAndThrow();

                    if (cards.Any(x => x.Id == cardData.Id)) throw new Exception($"duplicate card {cardData.Id} in {packPath}");

                    cardIds.Add(cardData.Id);
                    if (cardSet.SkipCompilePacks.IsNullOrEmpty() || !cardSet.SkipCompilePacks.Any(x => x == packPath))
                        cards.Enqueue(cardData);
                }
            }

            #endregion

            #region [prepare folder & cardDb]

            // create folder
            if (!Directory.Exists(config.PicPath)) Directory.CreateDirectory(config.PicPath);
            if (!Directory.Exists(config.PicFieldPath)) Directory.CreateDirectory(config.PicFieldPath);
            if (!Directory.Exists(config.ScriptPath)) Directory.CreateDirectory(config.ScriptPath);

            // clear card db
            using var db = new DataContext(cardSet.CardDbPath);
            {
                await db.Database.EnsureCreatedAsync();
                if (cardIds.Any())
                {
                    db.Data.RemoveRange(db.Data.Where(x => !cardIds.Contains(x.Id.Value)));
                    db.Text.RemoveRange(db.Text.Where(x => !cardIds.Contains(x.Id.Value)));
                }
                else
                {
                    db.Data.RemoveRange(db.Data);
                    db.Text.RemoveRange(db.Text);
                }
                await db.SaveChangesAsync();
            }

            // clear pic
            var unusedPics = Directory.GetFiles(config.PicPath)
                .Where(filePath => !cardIds.Any(id => filePath == Path.Combine(config.PicPath, $"{id}.png")));
            foreach (var filePath in unusedPics)
                File.Delete(filePath);

            // clear script
            var unusedScripts = Directory.GetFiles(config.ScriptPath)
                .Where(filePath => !utilities.ContainsKey(Path.GetFileName(filePath)))
                .Where(filePath => !cardIds.Any(id => filePath == Path.Combine(config.ScriptPath, $"c{id}.lua")));
            foreach (var filePath in unusedScripts)
                File.Delete(filePath);

            #endregion

            #region [write utility scripts]

            foreach (var utility in utilities)
                await CopyFile(utility.Value, Path.Combine(config.ScriptPath, Path.GetFileName(utility.Key)));

            #endregion

            #region [compile card]

            var tasks = new List<Task>();
            while (cards.Any())
            {
                var card = cards.Dequeue();
                tasks.Add(Task.Run(async () => await CompileCard(card, cardSet)));

                if (tasks.Count >= maxThread) await Task.WhenAny(tasks.ToArray());
                tasks.RemoveAll(x => x.Status == TaskStatus.RanToCompletion);
            }

            await Task.WhenAll(tasks.ToArray());

            #endregion
        }

        protected async Task CompileCard(CardDataDto card, CardSetDto cardSet)
        {
            Logger.LogInformation($"Compile card {card.Id}...");

            var config = new CardSetConfig(cardSet);
            await config.LoadMarco(card);

            card.Flavor = config.ApplyMarco(card.Flavor);
            card.Effect = config.ApplyMarco(card.Effect);
            card.PendulumEffect = config.ApplyMarco(card.PendulumEffect);
            if (!card.Strings.IsNullOrEmpty())
                card.Strings = card.Strings.Select(str => config.ApplyMarco(str)).ToArray()!;

            using var db = new DataContext(cardSet.CardDbPath!);
            {
                await WriteCardDb(card, config, db);
                if (card.GenerateScript) await GenerateCardScript(card, config);
                if (card.GeneratePic)
                {
                    await GenerateCardImage(card, config);
                    if (card.IsSpellType(SpellTypes.Field))
                        await DrawFieldArtwork(card, config);
                }
            }
        }

        protected async Task CopyFile(string sourceFilePath, string destinationFilePath)
        {
            using var sourceFile = File.OpenRead(sourceFilePath);
            using var destinationFile = new FileStream(destinationFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            destinationFile.SetLength(0);
            await sourceFile.CopyToAsync(destinationFile);
        }

        protected async Task WriteCardDb(CardDataDto card, CardSetConfig config, DataContext db)
        {
            var data = new Data { Id = card.Id };
            var text = new Text { Id = card.Id };

            data.Alias = card.Alias ?? 0;
            data.Category = 0;

            data.Ot = 0;
            if (card.CardLimit.IsNullOrEmpty())
                data.Ot = (int)CardLimits.Custom;
            else
            {
                foreach (var cardLimit in card.CardLimit)
                    data.Ot += (int)cardLimit;
            }

            data.SetCode = 0;
            var setKeys = card.Set?.Split(" ", System.StringSplitOptions.RemoveEmptyEntries);
            if (!setKeys.IsNullOrEmpty())
            {
                var count = 0;
                foreach (var key in setKeys)
                {
                    if (!config.SetCodes.ContainsKey(key)) continue;
                    data.SetCode += (int)(config.SetCodes[key] << (count * 16));
                    count += 1;
                }
            }

            data.Type = (int)card.CardType;
            if (card.IsSpellTrap)
            {
                if (card.SpellType.HasValue)
                    data.Type += (int)card.SpellType;
            }
            else if (card.IsMonster && !card.MonsterType.IsNullOrEmpty())
            {
                foreach (var type in card.MonsterType)
                    data.Type += (int)type;
            }

            data.Level = 0;
            if (card.IsMonster)
            {
                if (card.IsMonsterType(MonsterTypes.Link))
                    data.Level = card.LinkRating ?? 0;
                else if (card.IsMonsterType(MonsterTypes.Xyz))
                    data.Level = card.Rank ?? 0;
                else
                    data.Level = card.Level ?? 0;

                if (card.IsMonsterType(MonsterTypes.Pendulum))
                    data.Level += ((card.LeftScale ?? 0) << 24) + ((card.RightScale ?? 0) << 16);
            }

            data.Attribute = 0;
            if (!card.Attribute.IsNullOrEmpty())
            {
                foreach (var attribute in card.Attribute)
                    data.Attribute += (int)attribute;
            }

            data.Race = 0;
            if (!card.Race.IsNullOrEmpty())
            {
                foreach (var race in card.Race)
                    data.Race += (int)race;
            }

            if (card.IsMonster)
            {
                data.Atk = card.Atk ?? -2;
                data.Def = card.Def ?? -2;
            }

            if (card.IsLink)
            {
                data.Def = 0;
                if (!card.LinkArrow.IsNullOrEmpty())
                {
                    foreach (var arrow in card.LinkArrow)
                        data.Def += (int)arrow;
                }
            }

            text.Name = card.Name;
            if (!card.Effect.IsNullOrWhiteSpace() || !card.PendulumEffect.IsNullOrWhiteSpace() || !card.Flavor.IsNullOrWhiteSpace())
            {
                if (card.IsMonsterType(MonsterTypes.Pendulum))
                {
                    if (card.PendulumEffect.IsNullOrWhiteSpace())
                    {
                        text.Desc = card.Effect ?? card.Flavor;
                    }
                    else
                    {
                        text.Desc = "[ Pendulum Effect ]\n" + card.PendulumEffect + "\n";
                        if (!card.Effect.IsNullOrWhiteSpace())
                        {
                            text.Desc += "----------------------------------------\n"
                                + "[ Monster Effect ]\n"
                                + card.Effect;
                        }
                        else
                        {
                            text.Desc += "----------------------------------------\n"
                                + "[ Flavor Text ]\n"
                                + card.Flavor;
                        }
                    }
                }
                else
                {
                    text.Desc = config.ApplyMarco(card.Effect) ?? config.ApplyMarco(card.Flavor);
                }
            }

            if (!card.Strings.IsNullOrEmpty())
            {
                for (var i = 1; i <= card.Strings.Length; i++)
                    textEntityType.GetProperty("Str" + i)!.SetValue(text, config.ApplyMarco(card.Strings[i - 1]));
            }

            if (await db.Data.AnyAsync(x => x.Id == card.Id)) db.Update(data);
            else db.Add(data);
            if (await db.Text.AnyAsync(x => x.Id == card.Id)) db.Update(text);
            else db.Add(text);
            await db.SaveChangesAsync();
        }

        protected async Task GenerateCardScript(CardDataDto card, CardSetConfig config)
        {
            if (!Directory.Exists(Path.GetDirectoryName(card.ScriptPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(card.ScriptPath)!);
            if (!File.Exists(card.ScriptPath))
            {
                var scripts = new[]
                {
                    $"-- {card.Name}",
                    "Duel.LoadScript(\"util.lua\")",
                    "local s, id = GetID()",
                    "",
                    "function s.initial_effect(c)",
                    "    ",
                    "end",
                };
                await File.WriteAllLinesAsync(card.ScriptPath!, scripts);
            }

            using var inputFile = File.OpenRead(card.ScriptPath!);
            using var outputFile = File.Open(Path.Combine(config.ScriptPath, $"c{card.Id}.lua"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
            outputFile.SetLength(0);
            await inputFile.CopyToAsync(outputFile);
        }

        protected async Task GenerateCardImage(CardDataDto card, CardSetConfig config)
        {
            using var surface = SKSurface.Create(new SKImageInfo(config.CardWidth, config.CardHeight));
            var outputFilename = Path.Combine(config.PicPath, $"{card.Id}");
            var canvas = surface.Canvas;

            if (card.Template == CardTemplates.Artwork)
            {
                using var paint = new SKPaint();
                {
                    paint.IsAntialias = true;
                    paint.FilterQuality = SKFilterQuality.High;

                    var artworkImage = SKImage.FromEncodedData(await File.ReadAllBytesAsync(card.ArtworkPath!));
                    canvas.DrawImage(artworkImage, new SKRectI(0, 0, config.CardWidth, config.CardHeight), paint);
                    await SaveImage(surface, outputFilename);
                    return;
                }
            }

            if (card.IsMonsterType(MonsterTypes.Pendulum) && card.PendulumSize == PendulumSizes.Auto)
                card.PendulumSize = PendulumSizes.Medium;

            await DrawCardType(canvas, card, config);
            await DrawCardArtwork(canvas, card, config);
            await DrawCardFrame(canvas, card, config);
            await DrawCardName(canvas, card, config);
            if (card.IsSpellTrap)
            {
                await DrawSpellTrapType(canvas, card, config);
                await DrawSpellTrapEffect(canvas, card, config);
            }
            else if (card.IsMonster)
            {
                await DrawMonsterAttribute(canvas, card, config);
                await DrawMonsterLevelRankLink(canvas, card, config);
                await DrawMonsterPendulumScale(canvas, card, config);
                await DrawMonsterRace(canvas, card, config);
                await DrawMonsterAtkDef(canvas, card, config);
                await DrawMonsterEffect(canvas, card, config);
                await DrawMonsterPendulumEffect(canvas, card, config);
            }
            if (card.IsLink) await DrawLinkArrow(canvas, card, config);

            await SaveImage(surface, outputFilename);
        }

        protected async Task DrawCardType(SKCanvas canvas, CardDataDto card, CardSetConfig config)
        {
            using var paint = new SKPaint();
            paint.FilterQuality = SKFilterQuality.High;
            paint.IsAntialias = true;

            if (card.FramePath.IsNullOrWhiteSpace())
            {
                using var cardType = GetResourceImage("card_type", card.IsSpellTrap
                ? card.CardType.ToString("D")
                : card.MonsterPrimaryTypes![0].ToString("D"));
                canvas.DrawImage(cardType, new SKRectI(0, 0, config.CardWidth, config.CardHeight), paint);
            }
            else
            {
                using var sourceBitmap = SKBitmap.Decode(await File.ReadAllBytesAsync(card.FramePath));
                using var scaledBitmap = sourceBitmap.Resize(new SKImageInfo(694, 1013), SKFilterQuality.High);
                using var scaledImage = SKImage.FromBitmap(scaledBitmap);
                canvas.DrawImage(scaledImage, 0, 0, paint);
            }

            if (card.IsMonsterType(MonsterTypes.Pendulum))
            {
                using var pendulum = GetResourceImage("card_type", "16777216");
                canvas.DrawImage(pendulum, new SKRectI(0, 0, config.CardWidth, config.CardHeight), paint);
            }
        }

        protected async Task DrawCardArtwork(SKCanvas canvas, CardDataDto card, CardSetConfig config)
        {
            using var paint = new SKPaint();
            paint.FilterQuality = SKFilterQuality.High;
            paint.IsAntialias = true;

            using var sourceBitmap = SKBitmap.Decode(await File.ReadAllBytesAsync(card.ArtworkPath!));
            if (!card.IsMonsterType(MonsterTypes.Pendulum))
            {
                using var scaledBitmap = sourceBitmap.Resize(new SKImageInfo(528, 528), SKFilterQuality.High);
                using var scaledImage = SKImage.FromBitmap(scaledBitmap);
                canvas.DrawImage(scaledImage, 83, 186, paint);
            }
            else
            {
                var left = 44;
                var top = 182;
                var width = 605;
                var height = card.PendulumSize == PendulumSizes.Small ? 486 : 449;

                using var scaledBitmap = sourceBitmap.Resize(new SKImageInfo(width, sourceBitmap.Height), SKFilterQuality.High);
                using var scaledImage = SKImage.FromBitmap(scaledBitmap);
                using var cropSurface = SKSurface.Create(new SKImageInfo(width, height));
                using var cropCanvas = cropSurface.Canvas;
                cropCanvas.Translate(-left, -top);
                cropCanvas.DrawImage(scaledImage, left, top, paint);
                
                using var cropImage = cropSurface.Snapshot();
                canvas.DrawImage(cropImage, left, top, paint);

            }
        }

        protected Task DrawCardFrame(SKCanvas canvas, CardDataDto card, CardSetConfig config)
        {
            using var paint = new SKPaint();
            paint.FilterQuality = SKFilterQuality.High;
            paint.IsAntialias = true;

            var layout = "base";
            if (card.IsMonsterType(MonsterTypes.Pendulum))
                layout = "pendulum-" + card.PendulumSize.ToString().ToKebabCase();

            using var cardFrame = GetResourceImage("card_frame", card.Rarity.ToString().ToSnakeSpaceCase(), layout);
            canvas.DrawImage(cardFrame, new SKRectI(0, 0, config.CardWidth, config.CardHeight), paint);

            return Task.CompletedTask;
        }
        
        protected Task DrawCardName(SKCanvas canvas, CardDataDto card, CardSetConfig config)
        {
            using var paint = new SKPaint();
            paint.FilterQuality = SKFilterQuality.High;
            paint.IsAntialias = true;
            paint.Typeface = SKTypeface.FromFamilyName("Yu-Gi-Oh! Matrix Small Caps 1");
            paint.TextSize = 80;

            SKColor textColor = SKColors.Black;
            SKColor? shadowColor = null;

            if (card.NameTextColor.IsNullOrWhiteSpace())
            {
                if (card.Rarity == CardRarities.Gold)
                    textColor = new SKColor(117, 96, 5);
                else if (card.CardType == CardTypes.Spell
                    || card.CardType == CardTypes.Trap
                    || card.IsMonsterType(MonsterTypes.Xyz)
                    || card.IsMonsterType(MonsterTypes.Link))
                    textColor = SKColors.White;
            }
            else
            {
                textColor = SKColor.Parse(card.NameTextColor);
            }

            if (card.NameShadowColor.IsNullOrWhiteSpace())
            {
                if (card.Rarity == CardRarities.Gold)
                    shadowColor = new SKColor(231, 229, 228);
            }
            else if (!card.NameShadowColor.IsNullOrWhiteSpace())
            {
                shadowColor = SKColor.Parse(card.NameShadowColor);
            }

            var textBound = new SKRect();
            var textWidth = paint.MeasureText(card.Name, ref textBound);
            if (textWidth <= 525)
            {
                if (shadowColor.HasValue)
                {
                    paint.Color = shadowColor.Value;
                    canvas.DrawText(card.Name, 50, 100, paint);
                    paint.Color = textColor;
                    canvas.DrawText(card.Name, 48, 98, paint);
                }
                else
                {
                    paint.Color = textColor;
                    canvas.DrawText(card.Name, 50, 100, paint);
                }

                return Task.CompletedTask;
            }

            if (shadowColor.HasValue)
            {
                var shadowImageInfo = new SKImageInfo(525, 45);
                using var shadowSurface = SKSurface.Create(shadowImageInfo);
                using var shadowCanvas = shadowSurface.Canvas;
                shadowCanvas.Scale(shadowImageInfo.Width / textBound.Width, shadowImageInfo.Height / textBound.Height);
                shadowCanvas.Translate(-textBound.Left, -textBound.Top);
                {
                    paint.Color = shadowColor.Value;
                    shadowCanvas.DrawText(card.Name, 0, 0, paint);
                }
                using var shadowImage = shadowSurface.Snapshot();
                canvas.DrawImage(shadowImage, 52, 62, paint);
            }

            var textImageInfo = new SKImageInfo(525, 45);
            using var textSurface = SKSurface.Create(textImageInfo);
            using var textCanvas = textSurface.Canvas;
            textCanvas.Scale(textImageInfo.Width / textBound.Width, textImageInfo.Height / textBound.Height);
            textCanvas.Translate(-textBound.Left, -textBound.Top);
            {
                paint.Color = textColor;
                textCanvas.DrawText(card.Name, 0, 0, paint);
            }
            using var textImage = textSurface.Snapshot();
            canvas.DrawImage(textImage, 50, 60, paint);

            return Task.CompletedTask;
        }

        protected Task DrawSpellTrapType(SKCanvas canvas, CardDataDto card, CardSetConfig config)
        {
            if (card.IsLink || !card.SpellType.HasValue) return Task.CompletedTask;

            using var paint = new SKPaint();
            paint.FilterQuality = SKFilterQuality.High;
            paint.IsAntialias = true;

            if (card.IsSpellType(SpellTypes.Normal))
            {
                using var typeText = GetResourceImage("spelltrap_type", $"{card.CardType:D}-0");
                canvas.DrawImage(typeText, new SKRectI(0, 0, config.CardWidth, config.CardHeight), paint);
            }
            else
            {
                using var typeText = GetResourceImage("spelltrap_type", $"{card.CardType:D}-1");
                using var typeIcon = GetResourceImage("spelltrap_type", $"{card.SpellType:D}");
                canvas.DrawImage(typeText, new SKRectI(0, 0, config.CardWidth, config.CardHeight), paint);
                canvas.DrawImage(typeIcon, new SKRectI(0, 0, config.CardWidth, config.CardHeight), paint);
            }

            return Task.CompletedTask;
        }

        protected Task DrawSpellTrapEffect(SKCanvas canvas, CardDataDto card, CardSetConfig config)
        {
            var paint = new TextPaintOptions { Edging = SKFontEdging.SubpixelAntialias };
            var textblock = new TextBlock { MaxWidth = 593, MaxHeight = 190 };
            var point = new SKPoint { X = 50, Y = 760 };
            var style = new Style
            {
                TextColor = SKColors.Black,
                FontFamily = "Yu-Gi-Oh! Matrix Book",
                FontSize = 20,
            };

            textblock.AddText(card.Effect, style);
            while (textblock.Truncated && style.FontSize > 0)
            {
                style.FontSize -= 1;
                textblock.ApplyStyle(0, textblock.Length, style);
            }

            textblock.Paint(canvas, point, paint);
            return Task.CompletedTask;
        }

        protected Task DrawMonsterAttribute(SKCanvas canvas, CardDataDto card, CardSetConfig config)
        {
            if (card.Attribute.IsNullOrEmpty()) return Task.CompletedTask;

            using var paint = new SKPaint();
            paint.FilterQuality = SKFilterQuality.High;
            paint.IsAntialias = true;

            using var image = GetResourceImage("attribute", card.Attribute[0].ToString("D"));
            canvas.DrawImage(image, new SKRectI(0, 0, config.CardWidth, config.CardHeight), paint);
            return Task.CompletedTask;
        }

        protected Task DrawMonsterLevelRankLink(SKCanvas canvas, CardDataDto card, CardSetConfig config)
        {
            using var paint = new SKPaint();
            paint.FilterQuality = SKFilterQuality.High;
            paint.IsAntialias = true;
            paint.Typeface = SKTypeface.FromFamilyName("EurostileCandyW01");
            paint.FakeBoldText = true;
            paint.TextSize = 26;

            if (card.IsMonsterType(MonsterTypes.Link) && card.LinkRating.HasValue)
            {
                using var image = GetResourceImage("link-label");
                canvas.DrawImage(image, new SKRectI(0, 0, config.CardWidth, config.CardHeight), paint);
                canvas.DrawText(card.LinkRating.ToString(), 617, 947, paint);
            }
            else if (card.IsMonsterType(MonsterTypes.Xyz) && card.Rank.HasValue && card.Rank > 0 && card.ShowRank)
            {
                using var image = GetResourceImage("level_rank", $"rnk{card.Rank}");
                canvas.DrawImage(image, new SKRectI(0, 0, config.CardWidth, config.CardHeight), paint);
            }
            else if (card.Level.HasValue && card.Level > 0 && card.ShowLevel)
            {
                using var image = GetResourceImage("level_rank", $"lvl{card.Level}");
                canvas.DrawImage(image, new SKRectI(0, 0, config.CardWidth, config.CardHeight), paint);
            }

            if (card.IsMonsterType(MonsterTypes.Pendulum))
            {

            }
            
            return Task.CompletedTask;
        }

        protected Task DrawMonsterPendulumScale(SKCanvas canvas, CardDataDto card, CardSetConfig config)
        {
            if (!card.IsMonsterType(MonsterTypes.Pendulum)) return Task.CompletedTask;

            using var paint = new SKPaint();
            paint.FilterQuality = SKFilterQuality.High;
            paint.IsAntialias = true;
            paint.Typeface = SKTypeface.FromFamilyName("Yu-Gi-Oh! Matrix Small Caps 1");
            paint.TextSize = 60;

            var top = 730;
            if (card.PendulumSize == PendulumSizes.Large) top = 750;
            else if (card.PendulumSize == PendulumSizes.Small) top = 745;

            var leftScale = card.LeftScale?.ToString() ?? "?";
            var leftPos = 60 - ((leftScale.Length - 1) * 15);
            canvas.DrawText(leftScale, leftPos, top, paint);

            var rightScale = card.RightScale?.ToString() ?? "?";
            var rightPos = 613 - ((leftScale.Length - 1) * 15);
            canvas.DrawText(rightScale, rightPos, top, paint);

            return Task.CompletedTask;
        }

        protected Task DrawMonsterRace(SKCanvas canvas, CardDataDto card, CardSetConfig config)
        {
            using var paint = new SKPaint();
            paint.FilterQuality = SKFilterQuality.High;
            paint.IsAntialias = true;
            paint.Typeface = SKTypeface.FromFamilyName("Yu-Gi-Oh!ITCStoneSerifSmallCaps");
            paint.TextSize = 25;
            paint.Color = SKColors.Black;

            var monsterTypes = new List<Enum>();
            if (!card.Race.IsNullOrEmpty())
            {
                foreach (var race in card.Race)
                    monsterTypes.Add(race);
            }
            if (!card.MonsterPrimaryTypes.IsNullOrEmpty() && card.MonsterPrimaryTypes.Any(x => x != MonsterTypes.Effect && x != MonsterTypes.Normal))
            {
                foreach (var type in card.MonsterPrimaryTypes)
                {
                    if (type == MonsterTypes.Effect || type == MonsterTypes.Normal) continue;
                    monsterTypes.Add(type);
                }
            }
            if (!card.MonsterSecondaryTypes.IsNullOrEmpty())
            {
                if (card.IsMonsterType(MonsterTypes.Pendulum))
                    monsterTypes.Add(MonsterTypes.Pendulum);
                foreach (var type in card.MonsterSecondaryTypes)
                {
                    if (type == MonsterTypes.Pendulum) continue;
                    monsterTypes.Add(type);
                }
            }
            if (card.IsMonsterType(MonsterTypes.Effect))
                monsterTypes.Add(MonsterTypes.Effect);
            else if (card.IsMonsterType(MonsterTypes.Normal))
                monsterTypes.Add(MonsterTypes.Normal);

            var monsterTypeText = $"[{string.Join("/", monsterTypes.Select(x => Helpers.GetEnumDescription(x.GetType(), x) ))}]";

            var top = 785;
            if (card.IsMonsterType(MonsterTypes.Pendulum) && card.PendulumSize == PendulumSizes.Large)
                top = 810;

            var textBound = new SKRect();
            var textWidth = paint.MeasureText(monsterTypeText, ref textBound);
            if (textWidth <= 595)
            {
                canvas.DrawText(monsterTypeText, 50, top, paint);
                return Task.CompletedTask;
            }

            var textImageInfo = new SKImageInfo(588, 24);
            using var textSurface = SKSurface.Create(textImageInfo);
            using var textCanvas = textSurface.Canvas;
            textCanvas.Scale(textImageInfo.Width / textBound.Width, textImageInfo.Height / textBound.Height);
            textCanvas.Translate(-textBound.Left, -textBound.Top);
            textCanvas.DrawText(monsterTypeText, 0, 0, paint);
            using var textImage = textSurface.Snapshot();
            canvas.DrawImage(textImage, 52, top - 19, paint);
            return Task.CompletedTask;
        }

        protected Task DrawMonsterAtkDef(SKCanvas canvas, CardDataDto card, CardSetConfig config)
        {
            using var paint = new SKPaint();
            paint.FilterQuality = SKFilterQuality.High;
            paint.IsAntialias = true;
            paint.Typeface = SKTypeface.FromFamilyName("MatrixBoldSmallCaps");
            paint.TextSize = 28;
                        
            using var image = GetResourceImage("atkdef-line");
            canvas.DrawImage(image, new SKRectI(0, 0, config.CardWidth, config.CardHeight), paint);

            var atk = (card.Atk?.ToString() ?? "?").PadLeft(4, ' ');
            canvas.DrawText("ATK/", 385, 947, paint);
            canvas.DrawText(atk, 440, 947, paint);

            if (!card.IsLink)
            {
                var def = (card.Def?.ToString() ?? "?").PadLeft(4, ' ');
                canvas.DrawText("DEF/", 530, 947, paint);
                canvas.DrawText(def, 585, 947, paint);
            }

            return Task.CompletedTask;
        }

        protected Task DrawMonsterEffect(SKCanvas canvas, CardDataDto card, CardSetConfig config)
        {
            var paint = new TextPaintOptions { Edging = SKFontEdging.SubpixelAntialias };
            var textblock = new TextBlock { MaxWidth = 593, MaxHeight = 130 };
            var point = new SKPoint { X = 50, Y = 790 };
            var style = new Style
            {
                TextColor = SKColors.Black,
                FontFamily = card.Effect.IsNullOrWhiteSpace() ? "Yu-Gi-Oh! StoneSerif LT" : "Yu-Gi-Oh! Matrix Book",
                FontSize = 20,
            };

            if (card.IsMonsterType(MonsterTypes.Pendulum) && card.PendulumSize == PendulumSizes.Large)
            {
                point.Y = 815;
                textblock.MaxHeight = 105;
            }

            textblock.AddText(card.Effect ?? card.Flavor, style);
            while (textblock.Truncated && style.FontSize > 0)
            {
                style.FontSize -= 1;
                textblock.ApplyStyle(0, textblock.Length, style);
            }

            textblock.Paint(canvas, point, paint);
            return Task.CompletedTask;
        }

        protected Task DrawMonsterPendulumEffect(SKCanvas canvas, CardDataDto card, CardSetConfig config)
        {
            if (!card.IsMonsterType(MonsterTypes.Pendulum) || card.PendulumEffect.IsNullOrWhiteSpace()) return Task.CompletedTask;
            
            var paint = new TextPaintOptions { Edging = SKFontEdging.SubpixelAntialias };
            var textblock = new TextBlock { MaxWidth = 483, MaxHeight = 115 };
            var point = new SKPoint { X = 105, Y = 635 };
            var style = new Style
            {
                TextColor = SKColors.Black,
                FontFamily = "Yu-Gi-Oh! Matrix Book",
                FontSize = 20,
            };

            if (card.PendulumSize == PendulumSizes.Large)
            {
                textblock.MaxHeight = 140;
            }
            else if (card.PendulumSize == PendulumSizes.Small)
            {
                point.Y = 670;
                textblock.MaxHeight = 80;
            }

            textblock.AddText(card.PendulumEffect, style);
            while (textblock.Truncated && style.FontSize > 0)
            {
                style.FontSize -= 1;
                textblock.ApplyStyle(0, textblock.Length, style);
            }

            textblock.Paint(canvas, point, paint);
            return Task.CompletedTask;
        }

        protected Task DrawLinkArrow(SKCanvas canvas, CardDataDto card, CardSetConfig config)
        {
            if (!card.IsLink) return Task.CompletedTask;

            using var paint = new SKPaint();
            paint.FilterQuality = SKFilterQuality.High;
            paint.IsAntialias = true;

            var offPostfix = card.IsMonsterType(MonsterTypes.Pendulum) ? "pendulum-off" : "off";
            foreach (var linkArrow in Enum.GetValues<LinkArrows>())
            {
                using var arrowImage = GetResourceImage("link_arrow", card.Rarity.ToString().ToSnakeSpaceCase(), $"{linkArrow:D}-{offPostfix}");
                canvas.DrawImage(arrowImage, new SKRectI(0, 0, config.CardWidth, config.CardHeight), paint);
            }

            var onPostfix = card.IsMonsterType(MonsterTypes.Pendulum) ? "pendulum-on" : "on";
            if (!card.LinkArrow.IsNullOrEmpty())
            {
                foreach (var linkArrow in card.LinkArrow)
                {
                    using var arrowImage = GetResourceImage("link_arrow", card.Rarity.ToString().ToSnakeSpaceCase(), $"{linkArrow:D}-{onPostfix}");
                    canvas.DrawImage(arrowImage, new SKRectI(0, 0, config.CardWidth, config.CardHeight), paint);
                }
            }

            return Task.CompletedTask;
        }

        protected async Task DrawFieldArtwork(CardDataDto card, CardSetConfig config)
        {
            using var paint = new SKPaint();
            paint.FilterQuality = SKFilterQuality.High;
            paint.IsAntialias = true;
            var filename = Path.Combine(config.PicFieldPath, $"{card.Id}.png");

            using var sourceBitmap = SKBitmap.Decode(await File.ReadAllBytesAsync(card.ArtworkPath!));
            using var scaledBitmap = sourceBitmap.Resize(new SKImageInfo(Math.Min(512, sourceBitmap.Width), Math.Min(512, sourceBitmap.Height)), SKFilterQuality.High);
            using var scaledImage = SKImage.FromBitmap(scaledBitmap);
            using var stream = new MemoryStream(scaledImage.Encode(SKEncodedImageFormat.Png, 100).ToArray());
            using var file = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            await stream.CopyToAsync(file);
        }

        protected async Task SaveImage(SKSurface surface, string filename)
        {
            filename = Path.ChangeExtension(filename, "png");
            using var image = surface.Snapshot();
            using var stream = new MemoryStream(image.Encode(SKEncodedImageFormat.Png, 100).ToArray());
            using var file = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            await stream.CopyToAsync(file);
        }
    
        protected SKImage GetResourceImage(params string[] keys)
        {
            return SKImage.FromEncodedData(_assembly.GetManifestResourceStream($"YgoCardGenerator.Resources.Proxy.{string.Join(".", keys)}.png"));
        }
    }
}
