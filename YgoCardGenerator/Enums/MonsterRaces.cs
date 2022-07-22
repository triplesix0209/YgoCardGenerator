using System.ComponentModel;

namespace YgoCardGenerator.Enums
{
    public enum MonsterRaces
    {
        Warrior = 0x1,

        Spellcaster = 0x2,

        Fairy = 0x4,

        Fiend = 0x8,

        Zombie = 0x10,

        Machine = 0x20,

        Aqua = 0x40,

        Pyro = 0x80,

        Rock = 0x100,

        [Description("Winged-Beast")]
        WingedBeast = 0x200,

        Plant = 0x400,

        Insect = 0x800,

        Thunder = 0x1000,

        Dragon = 0x2000,

        Beast = 0x4000,

        [Description("Beast-Warrior")]
        BeastWarrior = 0x8000,

        Dinosaur = 0x10000,

        Fish = 0x20000,

        [Description("Sea Serpent")]
        SeaSerpent = 0x40000,

        Reptile = 0x80000,

        Psychic = 0x100000,

        [Description("Divine-Beast")]
        DivineBeast = 0x200000,

        [Description("Creator God")]
        CreatorGod = 0x400000,

        Wyrm = 0x800000,

        Cyberse = 0x1000000,

        Cyborg = 0x2000000,

        [Description("Magical Knight")]
        MagicalKnight = 0x4000000,

        [Description("High Dragon")]
        HighDragon = 0x8000000,

        [Description("Omega Psychic")]
        OmegaPsychic = 0x10000000,

        [Description("Celestial Warrior")]
        CelestialWarrior = 0x20000000,
    }
}
