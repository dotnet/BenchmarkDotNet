using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Validators
{
    internal class CompositeValidator : IValidator
    {
        private static readonly IValidator[] MandatoryValidators = 
        {
            BaselineValidator.FailOnError,
            SetupCleanupValidator.FailOnError,
            UnrollFactorValidator.Default,
            DiagnosersValidator.Default,
            CompilationValidator.Default,
            ConfigValidator.Default,
            ShadowCopyValidator.Default
        };

        internal readonly IValidator[] Validators;

        public CompositeValidator(IValidator[] configuredValidators)
        {
            Validators = configuredValidators
                .Concat(MandatoryValidators)
                .GroupBy(validator => validator.GetType())
                .Select(groupedByType => groupedByType.FirstOrDefault(validator => validator.TreatsWarningsAsErrors) ?? groupedByType.First())
                .Distinct()
                .ToArray();
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