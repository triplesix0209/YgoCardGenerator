namespace YgoCardGenerator.Attribtues
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EnumInfoAttribute : Attribute
    {
        public uint Code { get; set; }

        public string? Text { get; set; }
    }
}
