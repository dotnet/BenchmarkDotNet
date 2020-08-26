using System.Linq;
using System.Collections.Generic;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Validators
{
    public class ConfigCompatibilityValidator : IValidator
    {
        public static readonly ConfigCompatibilityValidator FailOnError = new ConfigCompatibilityValidator();

        public bool TreatsWarningsAsErrors => true;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            var orderers =
                validationParameters
                    .Benchmarks
                    .Where(benchmark => benchmark.Config.Orderer != Order.DefaultOrderer.Instance)
                    .Select(benchmark => benchmark.Config.Orderer)
                    .Distinct();

            if (orderers.Count() > 1)
                yield return new ValidationError(true, "You use JoinSummary options, but provided configurations cannot be joined. Only one Orderer per benchmark cases is allowed.");
        }
    }
}
