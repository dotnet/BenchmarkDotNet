using System.Collections.Immutable;

namespace BenchmarkDotNet.Validators
{
    internal class CompositeValidator : IValidator
    {
        private readonly ImmutableHashSet<IValidator> validators;

        public CompositeValidator(ImmutableHashSet<IValidator> validators) => this.validators = validators;

        /// <summary>
        /// returns true if any of the validators has TreatsWarningsAsErrors == true
        /// </summary>
        public bool TreatsWarningsAsErrors
            => validators.Any(validator => validator.TreatsWarningsAsErrors);

        public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters)
            => validators.ToAsyncEnumerable().SelectMany(validator => validator.ValidateAsync(validationParameters)).Distinct();
    }
}