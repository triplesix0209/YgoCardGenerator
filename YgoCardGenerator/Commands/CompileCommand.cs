using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System.Text;
using YgoCardGenerator.Persistences;
using YgoCardGenerator.Persistences.Entities;

namespace YgoCardGenerator.Commands
{
    public class CompileCommand : AppCommand
    {
        private readonly static Type textEntityType = typeof(Text);

        public CompileCommand(string[] args, ILogger logger)
            : base(args, logger)
        {
        }

        protected override CommandArgument[] ArgumentSchema => new CommandArgument[]
        {
            new ("set") { Description = "path card set filename to compile (TOML)" },
        };

        public override async Task Do()
        {
            #region [read cardset]

            var cardSetFilename = Arguments[0].Value();
            var cardSet = Toml.ToModel<CardSetDto>(await File.ReadAllTextAsync(cardSetFilename!));
            cardSet.BasePath = Path.GetDirectoryName(cardSetFilename)!;
            cardSet.ValidateAndThrow();
            var config = new CardSetConfig(cardSet);

            #endregion

            #region [read card data]

            var cardPacks = new List<(string path, List<CardDataDto> cards)>();
            foreach (var packPath in cardSet.Packs!)
            {
                (string path, List<CardDataDto> cards) cardPack = new()
                {
                    path = Path.Combine(cardSet.BasePath!, packPath),
                    cards = new List<CardDataDto>()
                };
                
                var cards = Toml.ToModel(await File.ReadAllTextAsync(Path.Combine(cardPack.path, CardSetConfig.CardIndexFileName)));
                foreach (var card in cards.Values)
                {
                    var cardInput = Toml.ToModel<CardInputDto>(Toml.FromModel(card));
                    var cardData = cardInput.ToCardDataDto(cardPack.path);
                    cardData.ValidateAndThrow();
                    cardPack.cards.Add(cardData);
                }

                cardPacks.Add(cardPack);
            }

            #endregion

            #region [prepare folder & carddb]

            if (!Directory.Exists(config.ScriptPath)) Directory.CreateDirectory(config.ScriptPath);
            if (!Directory.Exists(config.PicPath)) Directory.CreateDirectory(config.PicPath);
            using var db = new DataContext(Path.Combine(cardSet.BasePath, $"{cardSet.SetName}.cdb"));
            await db.Database.EnsureCreatedAsync();

            #endregion

            #region [compile card]

            foreach (var filename in Directory.GetFiles(config.UtilityDirectory))
            {
                using var inputFile = new FileStream(filename, FileMode.Open, FileAccess.Read);
                using var outputFile = new FileStream(Path.Combine(config.ScriptPath, Path.GetFileName(filename)), FileMode.OpenOrCreate, FileAccess.ReadWrite);
                await inputFile.CopyToAsync(outputFile);
            }

            foreach (var (path, cards) in cardPacks)
            {
                foreach (var card in cards)
                {
                    await config.LoadMarco(path, card);
                    await WriteCardDb(card, config, db);
                    await GenerateCardScript(card, config);
                    await GenerateCardArtwork(card, config);
                }
            }

            #endregion

            #region [remove unused file]

            var utilScripts = Directory.GetFiles(config.UtilityDirectory);
            var unusedScripts = Directory.GetFiles(config.ScriptPath).Where(filename =>
            {
                foreach (var (path, cards) in cardPacks)
                foreach (var card in cards)
                    if (filename == Path.Combine(config.ScriptPath, $"c{card.Id}.lua")) return false;
                return utilScripts.Contains(filename);
            });

            var unusedPics = Directory.GetFiles(config.PicPath).Where(filename =>
            {
                foreach (var (path, cards) in cardPacks)
                    foreach (var card in cards)
                        if (filename == Path.Combine(config.PicPath, $"{card.Id}.jpg")) return false;
                return true;
            });

            foreach (var filename in unusedScripts) File.Delete(filename);
            foreach (var filename in unusedPics) File.Delete(filename);

            #endregion
        }

        protected async Task WriteCardDb(CardDataDto card, CardSetConfig config, DataContext db)
        {
            var data = new Data { Id = card.Id };
            var text = new Text { Id = card.Id };

            data.Ot = (int)card.CardLimit;
            data.Alias = card.Alias ?? 0;
            data.Category = 0;

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
                    var pendulumEffect = config.ApplyMarco(card.PendulumEffect);
                    var monsterEffect = config.ApplyMarco(card.Effect);
                    var flavorText = config.ApplyMarco(card.Flavor);

                    if (pendulumEffect.IsNullOrWhiteSpace())
                    {
                        text.Desc = monsterEffect ?? flavorText;
                    }
                    else
                    {
                        text.Desc = "[ Pendulum Effect ]\n" + pendulumEffect + "\n";
                        if (!monsterEffect.IsNullOrWhiteSpace())
                        {
                            text.Desc += "----------------------------------------\n"
                                + "[ Monster Effect ]\n"
                                + monsterEffect;
                        }
                        else
                        {
                            text.Desc += "----------------------------------------\n"
                                + "[ Flavor Text ]\n"
                                + flavorText;
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
            if (!File.Exists(card.ScriptPath))
            {
                var scripts = new[]
                {
                    $"-- {card.Name}",
                    "Duel.LoadScript(\"util.lua\")",
                    "",
                    "local s, id = GetID()",
                    "",
                    "function s.initial_effect(c)",
                    "    ",
                    "end",
                };
                await File.WriteAllLinesAsync(card.ScriptPath!, scripts, Encoding.UTF8);
            }

            using var inputFile = new FileStream(card.ScriptPath!, FileMode.Open, FileAccess.Read);
            using var outputFile = new FileStream(Path.Combine(config.ScriptPath, $"c{card.Id}.lua"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
            await inputFile.CopyToAsync(outputFile);
        }

        protected async Task GenerateCardArtwork(CardDataDto card, CardSetConfig config)
        {
            var imageInfo = new SKImageInfo(694, 1013);
            using var surface = SKSurface.Create(imageInfo);
            var outputFilename = Path.Combine(config.PicPath, $"{card.Id}.jpg");
            var canvas = surface.Canvas;

            if (card.Template == CardTemplates.Artwork)
            {
                using var paint = new SKPaint();
                {
                    paint.IsAntialias = true;
                    paint.FilterQuality = SKFilterQuality.High;

                    var artworkImage = SKImage.FromEncodedData(await File.ReadAllBytesAsync(card.ArtworkPath!));
                    canvas.DrawImage(artworkImage, new SKRectI(0, 0, imageInfo.Width, imageInfo.Height), paint);
                    await SaveImage(surface, outputFilename);
                    return;
                }
            }
        }

        protected async Task SaveImage(SKSurface surface, string filename)
        {
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 100);
            using var stream = new MemoryStream(data.ToArray());
            using var file = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            await stream.CopyToAsync(file);
        }
    }
}
