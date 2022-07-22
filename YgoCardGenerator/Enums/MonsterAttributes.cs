namespace YgoCardGenerator.Enums
{
    public enum CardLimits
    {
        OCG = 0x1,
        TCG = 0x2,
        Anime = 0x4,
        Illegal = 0x8,
        VideoGame = 0x10,
        Custom = 0x20,
        Speed = 0x40,
        PreRelease = 0x100,
        Rush = 0x200,
        Legend = 0x400,
        Hidden = 0x1000,
    }
}
