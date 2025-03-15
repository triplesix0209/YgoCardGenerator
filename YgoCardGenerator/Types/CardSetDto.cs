#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace YgoCardGenerator.Types
{
    public class CardSetDto : Dto<CardSetValidator>
    {
        public string SetName { get; set; }

        public string? BasePath { get; set; }

        public ExportTypes ExportType { get; set; }

        public string? GamePath { get; set; }

        public string? ExpansionPath { get; set; }

        public string? CloseupPath { get; set; }

        public string? CutinPath { get; set; }

        public string? PicFieldPath { get; set; }

        public string[]? Setcodes { get; set; }

        public string[]? Packs { get; set; }

        public string[]? SkipCompilePacks { get; set; }

        public string? CardDbPath { get; set; }
    }

    public class CardSetValidator : AbstractValidator<CardSetDto>
    {
        public CardSetValidator()
        {
            RuleFor(x => x.SetName)
                .NotEmpty();

            RuleFor(x => x.BasePath)
                .NotEmpty();

            RuleFor(x => x.ExportType)
                .IsInEnum();

            RuleFor(x => x.GamePath)
                .NotEmpty();

            RuleFor(x => x.ExpansionPath)
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
