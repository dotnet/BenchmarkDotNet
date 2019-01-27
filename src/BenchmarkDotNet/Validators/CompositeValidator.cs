using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Validators
{
    public class CompositeValidator : IValidator
    {
        internal readonly IValidator[] Validators;

        public CompositeValidator(IValidator[] configuredValidators)
        {
            Validators = configuredValidators;
        }

        /// <summary>
        /// returns true if any of the validators has TreatsWarningsAsErrors == true
        /// </summary>
        public bool TreatsWarningsAsErrors => Validators.Any(validator => validator.TreatsWarningsAsErrors);

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            return Validators.SelectMany(validator => validator.Validate(validationParameters));
        }
    }
}