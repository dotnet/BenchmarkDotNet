using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class CompositeValidator : IValidator
    {
        private static readonly IValidator[] MandatoryValidators = 
        {
            BaselineValidator.FailOnError
        };

        private readonly IValidator[] validators;

        public CompositeValidator(IValidator[] configuredValidators)
        {
            validators = configuredValidators
                .Concat(MandatoryValidators)
                .Distinct()
                .ToArray();
        }

        public IEnumerable<IValidationError> Validate(IList<Benchmark> benchmarks)
        {
            return validators.SelectMany(validator => validator.Validate(benchmarks));
        }
    }
}