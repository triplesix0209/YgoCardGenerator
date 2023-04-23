using YgoCardGenerator.Attribtues;

namespace YgoCardGenerator.Enums
{
    public enum MonsterRaces
    {
        [EnumInfo(Code = 0x1)]
        Warrior,

        [EnumInfo(Code = 0x2)]
        Spellcaster,

        [EnumInfo(Code = 0x4)]
        Fairy,

        [EnumInfo(Code = 0x8)]
        Fiend,

        [EnumInfo(Code = 0x10)]
        Zombie,

        [EnumInfo(Code = 0x20)]
        Machine,

        [EnumInfo(Code = 0x40)]
        Aqua,

        [EnumInfo(Code = 0x80)]
        Pyro,

        [EnumInfo(Code = 0x100)]
        Rock,

        [EnumInfo(Code = 0x200, Text = "Winged-Beast")]
        WingedBeast,

        [EnumInfo(Code = 0x400)]
        Plant,

        [EnumInfo(Code = 0x800)]
        Insect,

        [EnumInfo(Code = 0x1000)]
        Thunder,

        [EnumInfo(Code = 0x2000)]
        Dragon,

        [EnumInfo(Code = 0x4000)]
        Beast,

        [EnumInfo(Code = 0x8000, Text = "Beast-Warrior")]
        BeastWarrior,

        [EnumInfo(Code = 0x10000)]
        Dinosaur,

        [EnumInfo(Code = 0x20000)]
        Fish,

        [EnumInfo(Code = 0x40000, Text = "Sea Serpent")]
        SeaSerpent,

        [EnumInfo(Code = 0x80000)]
        Reptile,

        [EnumInfo(Code = 0x100000)]
        Psychic,

        [EnumInfo(Code = 0x200000, Text = "Divine-Beast")]
        DivineBeast = 0x200000,

        [EnumInfo(Code = 0x400000, Text = "Creator God")]
        CreatorGod,

        [EnumInfo(Code = 0x800000)]
        Wyrm,

        [EnumInfo(Code = 0x1000000)]
        Cyberse,

        [EnumInfo(Code = 0x2000000)]
        Illusionist,

        [EnumInfo(Code = 0x4000000)]
        Cyborg,

        [EnumInfo(Code = 0x8000000, Text = "Magical Knight")]
        MagicalKnight,

        [EnumInfo(Code = 0x10000000, Text = "High Dragon")]
        HighDragon,

        [EnumInfo(Code = 0x20000000, Text = "Omega Psychic")]
        OmegaPsychic,

        [EnumInfo(Code = 0x40000000, Text = "Celestial Warrior")]
        CelestialWarrior,

        [EnumInfo(Code = 0x80000000)]
        Galaxy,
    }
}
