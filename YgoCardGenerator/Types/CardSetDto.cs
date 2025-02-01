namespace YgoCardGenerator.Types
{
    public class CardSetDto : Dto<CardSetValidator>
    {
        public ExportTypes ExportType { get; set; }

        public string? BasePath { get; set; }

        public string? PicFieldPath { get; set; }

        public string? SetName { get; set; }

        public string[]? Setcodes { get; set; }

        public string[]? Packs { get; set; }

        public string[]? SkipCompilePacks { get; set; }

        public string? CardDbPath => BasePath == null ? null : Path.Combine(BasePath, $"{SetName}.cdb");
    }

    public class CardSetValidator : AbstractValidator<CardSetDto>
    {
        public CardSetValidator()
        {
            RuleFor(x => x.ExportType)
                .IsInEnum();

            RuleFor(x => x.BasePath)
                .NotEmpty();

            RuleFor(x => x.SetName)
                .NotEmpty();

            RuleFor(x => x.Setcodes)
                .Must(field => field == null || field.All(item => !item.IsNullOrWhiteSpace()))
                .WithMessage("Setcode list cannot be empty");
            RuleFor(x => x.Setcodes)
                .Must((model, field) => field == null || field.All(item => File.Exists(Path.Combine(model.BasePath!, item))))
                .WithMessage("Setcode file must be existed");

            RuleFor(x => x.Packs)
                .Must(field => field == null || field.All(item => !item.IsNullOrWhiteSpace()))
                .WithMessage("Pack list cannot be empty");
            RuleFor(x => x.Packs)
                .Must((model, field) => field == null || field.All(item => File.Exists(Path.Combine(model.BasePath!, item, CardSetConfig.CardIndexFileName))))
                .WithMessage("Pack files (pack.toml) must be existed");

            RuleFor(x => x.SkipCompilePacks)
                .Must((model, field) => field == null || field.All(item => File.Exists(Path.Combine(model.BasePath!, item, CardSetConfig.CardIndexFileName))))
                .WithMessage("Pack files (pack.toml) must be existed");
        }
    }
}
