using FluentValidation.Results;

namespace YgoCardGenerator.Types
{
    public abstract class Dto
    {
        protected abstract IValidator Validator { get; }

        public ValidationResult Validate()
        {
            return Validator.Validate(ValidationContext<Dto>.CreateWithOptions(this, _ => { }));
        }

        public void ValidateAndThrow()
        {
            Validator.Validate(ValidationContext<Dto>.CreateWithOptions(this, options => options.ThrowOnFailures()));
        }
    }
}
