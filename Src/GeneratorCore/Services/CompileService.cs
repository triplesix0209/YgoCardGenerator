using System.IO;
using System.Threading.Tasks;
using GeneratorCore.Database;
using GeneratorCore.Database.Entities;
using GeneratorCore.Dto;
using GeneratorCore.Enums;
using GeneratorCore.Helpers;
using GeneratorCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Tomlyn;
using TripleSix.Core.Helpers;
using TripleSix.Core.Services;

namespace GeneratorCore.Services
{
    public class CompileService : BaseService,
        ICompileService
    {
        public async Task Compile(string outputPath, CardSetDto setConfig)
        {
            var cardFilename = Path.Combine(outputPath, setConfig.SetName + ".cdb");
            File.Delete(cardFilename);

            var outputScriptPath = Path.Combine(outputPath, "script");
            if (Directory.Exists(outputScriptPath)) Directory.Delete(outputScriptPath, true);
            Directory.CreateDirectory(outputScriptPath);

            var db = new DataContext();
            await db.Database.EnsureCreatedAsync();
            foreach (var packPath in setConfig.Packs)
            {
                var fullPackPath = Path.Combine(setConfig.BasePath, packPath);
                var cards = Toml.ToModel(await File.ReadAllTextAsync(fullPackPath));
                foreach (var card in cards.Values)
                {
                    var model = Toml.ToModel<CardModelDto>(Toml.FromModel(card));
                    model.BasePath = Path.GetDirectoryName(fullPackPath);
                    await CompileCard(db, setConfig, model.ToDataDto());

                    var sourceScriptFilename = Path.Combine(model.BasePath, "script", $"c{model.Id}.lua");
                    if (!File.Exists(sourceScriptFilename))
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(sourceScriptFilename)))
                            Directory.CreateDirectory(Path.GetDirectoryName(sourceScriptFilename));

                        using (var file = File.CreateText(sourceScriptFilename))
                        {
                            file.WriteLine($"-- {model.Name}");
                            file.WriteLine($"local s, id = GetID()");
                            file.WriteLine();
                            file.WriteLine($"function s.initial_effect(c)");
                            file.WriteLine();
                            file.WriteLine($"end");
                        }
                    }

                    File.Copy(sourceScriptFilename, Path.Combine(outputScriptPath, $"c{model.Id}.lua"), true);
                }
            }

            var utilityPath = Path.Combine(setConfig.BasePath, "utility");
            if (Directory.Exists(utilityPath))
            {
                var files = Directory.GetFiles(utilityPath);
                foreach (var file in files)
                    File.Copy(file, Path.Combine(outputScriptPath, Path.GetFileName(file)), true);
            }

            File.Copy("cards.cdb", cardFilename, true);
        }

        protected async Task CompileCard(DataContext db, CardSetDto setConfig, CardDataDto input)
        {
            var marco = await setConfig.LoadMarco(input);
            var data = new DataEntity { Id = input.Id };
            var text = new TextEntity { Id = input.Id };

            data.Ot = (int)input.CardLimit;
            data.Alias = input.Alias ?? 0;
            data.Category = 0;

            data.SetCode = 0;
            var setKeys = input.Set?.Split(" ", System.StringSplitOptions.RemoveEmptyEntries);
            if (setKeys.IsNotNullOrEmpty())
            {
                var setCodes = await setConfig.LoadSetcode();
                var count = 0;
                foreach (var key in setKeys)
                {
                    if (!setCodes.ContainsKey(key)) continue;
                    data.SetCode += (int)(setCodes[key] << (count * 16));
                    count += 1;
                }
            }

            data.Type = (int)input.CardType;
            if (input.IsSpellTrap)
            {
                data.Type += (int)input.SpellType;
            }
            else if (input.IsMonster && input.MonsterType.IsNotNullOrEmpty())
            {
                foreach (var type in input.MonsterType)
                    data.Type += (int)type;
            }

            data.Level = 0;
            if (input.IsMonster)
            {
                if (input.IsMonsterType(MonsterTypes.Link))
                    data.Level = input.LinkRating ?? 0;
                else if (input.IsMonsterType(MonsterTypes.Xyz))
                    data.Level = input.Rank ?? 0;
                else
                    data.Level = input.Level ?? 0;

                if (input.IsMonsterType(MonsterTypes.Pendulum))
                    data.Level += ((input.LeftScale ?? 0) << 24) + ((input.RightScale ?? 0) << 16);
            }

            data.Attribute = 0;
            if (input.Attribute.IsNotNullOrEmpty())
            {
                foreach (var attribute in input.Attribute)
                    data.Attribute += (int)attribute;
            }

            data.Race = 0;
            if (input.Race.IsNotNullOrEmpty())
            {
                foreach (var race in input.Race)
                    data.Race += (int)race;
            }

            if (input.IsMonster)
            {
                data.Atk = input.Atk ?? -2;
                data.Def = input.Def ?? -2;
            }

            if (input.IsLink)
            {
                data.Def = 0;
                if (input.LinkArrow.IsNotNullOrEmpty())
                {
                    foreach (var arrow in input.LinkArrow)
                        data.Def += (int)arrow;
                }
            }

            text.Name = input.Name;
            if (input.Effect.IsNotNullOrWhiteSpace() || input.PendulumEffect.IsNotNullOrWhiteSpace() || input.Flavor.IsNotNullOrWhiteSpace())
            {
                if (input.IsMonsterType(MonsterTypes.Pendulum))
                {
                    var pendulumEffect = input.PendulumEffect?.ApplyMarco(marco);
                    var monsterEffect = input.Effect?.ApplyMarco(marco);
                    var flavorText = input.Flavor?.ApplyMarco(marco);

                    if (pendulumEffect.IsNullOrWhiteSpace())
                    {
                        text.Desc = monsterEffect ?? flavorText;
                    }
                    else
                    {
                        text.Desc = "[ Pendulum Effect ]\n" + pendulumEffect + "\n";
                        if (monsterEffect.IsNotNullOrWhiteSpace())
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
                    text.Desc = input.Effect?.ApplyMarco(marco) ?? input.Flavor?.ApplyMarco(marco);
                }
            }

            if (input.Strings.IsNotNullOrEmpty())
            {
                for (var i = 1; i <= input.Strings.Length; i++)
                    text.GetType().GetProperty("Str" + i).SetValue(text, input.Strings[i - 1].ApplyMarco(marco));
            }

            if (await db.Data.AnyAsync(x => x.Id == input.Id)) db.Update(data);
            else db.Add(data);
            if (await db.Text.AnyAsync(x => x.Id == input.Id)) db.Update(text);
            else db.Add(text);
            await db.SaveChangesAsync();
        }
    }
}
