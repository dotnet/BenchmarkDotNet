using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class BaselineValidator : IValidator
    {
        private const bool AlwaysFailOnError = true; // it is a must!

        public static readonly BaselineValidator FailOnError = new BaselineValidator();

        private BaselineValidator() { }

        public IEnumerable<IValidationError> Validate(IList<Benchmark> benchmarks)
        {
            foreach (var groupByType in benchmarks.GroupBy(benchmark => benchmark.Target.Type))
            {
                var allMethods = groupByType.Key.GetAllMethods();

                if (allMethods.Count(method => method.GetCustomAttributes<BenchmarkAttribute>(false).Any(benchmarkAttribute => benchmarkAttribute.Baseline)) > 1)
                {
                    yield return new ValidationError(
                        AlwaysFailOnError,
                        $"Only 1 [Benchmark] in a class can have \"Baseline = true\" applied to it, class {groupByType.Key.Name} has few");
                }
            }
        }
    }
}