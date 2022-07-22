using FluentValidation.Results;

namespace YgoCardGenerator.Types
{
    public abstract class Dto
    {
    }

    public abstract class Dto<TValidator> : Dto
        where TValidator : IValidator, new()
    {
        public ValidationResult Validate()
        {
            return new TValidator().Validate(ValidationContext<Dto>.CreateWithOptions(this, _ => { }));
        }

        public void ValidateAndThrow()
        {
            new TValidator().Validate(ValidationContext<Dto>.CreateWithOptions(this, options => options.ThrowOnFailures()));
        }
    }
}
