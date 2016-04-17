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

        internal readonly IValidator[] Validators;

        public CompositeValidator(IValidator[] configuredValidators)
        {
            Validators = configuredValidators
                .Concat(MandatoryValidators)
                .GroupBy(valiadator => valiadator.GetType())
                .Select(grouppedByType => grouppedByType.FirstOrDefault(validator => validator.TreatsWarningsAsErrors) ?? grouppedByType.First())
                .Distinct()
                .ToArray();
        }

        public bool TreatsWarningsAsErrors => Validators.All(validator => validator.TreatsWarningsAsErrors);

        public IEnumerable<IValidationError> Validate(IList<Benchmark> benchmarks)
        {
            return Validators.SelectMany(validator => validator.Validate(benchmarks));
        }
    }
}