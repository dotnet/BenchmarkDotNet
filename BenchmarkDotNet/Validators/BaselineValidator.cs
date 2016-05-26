using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class BaselineValidator : IValidator
    {
        public static readonly BaselineValidator FailOnError = new BaselineValidator();

        private BaselineValidator() { }

        public bool TreatsWarningsAsErrors => true; // it is a must!

        public IEnumerable<ValidationError> Validate(IList<Benchmark> benchmarks)
        {
            foreach (var groupByType in benchmarks.GroupBy(benchmark => benchmark.Target.Type))
            {
                var allMethods = groupByType.Key.GetAllMethods();
                var count = allMethods.Count(method => method.GetCustomAttributes<BenchmarkAttribute>(false)
                                                             .Any(benchmarkAttribute => benchmarkAttribute.Baseline));
                if (count > 1)
                {
                    yield return new ValidationError(
                        TreatsWarningsAsErrors,
                        $"Only 1 [Benchmark] in a class can have \"Baseline = true\" applied to it, class {groupByType.Key.Name} has {count}");
                }
            }
        }
    }
}