namespace YgoCardGenerator.Enums
{
    public enum SpellTypes
    {
        Normal = 0,
        Ritual = 0x80,
        QuickPlay = 0x10000,
        Continuous = 0x20000,
        Equip = 0x40000,
        Field = 0x80000,
        Counter = 0x100000,
        Link = 0x4000000,
    }
}
