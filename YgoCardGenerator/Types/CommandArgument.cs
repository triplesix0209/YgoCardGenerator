namespace YgoCardGenerator.Types
{
    public class CommandArgument
    {
        private readonly string? _value;

        public CommandArgument(string key)
        {
            Key = key;
        }

        public CommandArgument(string key, params string[] flags)
        {
            Key = key;
            Flags = flags.Select(x => x.ToKebabCase()).ToArray();
        }

        public CommandArgument(CommandArgument schema, string? value)
        {
            Key = schema.Key;
            Flags = schema.Flags;
            DefaultValue = schema.DefaultValue;
            Description = schema.Description;
            _value = value;
        }

        public string Key { get; }

        public string[]? Flags { get; }

        public string? DefaultValue { get; set; }

        public string? Description { get; set; }

        public bool IsRequired => DefaultValue is null;

        public string? Value() => _value;

        public T? Value<T>()
        {
            if (_value == null) return default;
            return (T)Convert.ChangeType(_value, typeof(T));
        }
    }
}
