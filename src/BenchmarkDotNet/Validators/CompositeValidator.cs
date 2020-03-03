using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => validators.SelectMany(validator => validator.Validate(validationParameters)).Distinct();
    }
}