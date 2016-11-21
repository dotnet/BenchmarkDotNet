using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Validators
{
    public class BaselineValidator : IValidator
    {
        public static readonly BaselineValidator FailOnError = new BaselineValidator();

        private BaselineValidator() { }

        public bool TreatsWarningsAsErrors => true; // it is a must!

        public IEnumerable<ValidationError> Validate(ValidationParameters input)
        {
            foreach (var groupByType in input.Benchmarks.GroupBy(benchmark => benchmark.Target.Type))
            {
                var allMethods = groupByType.Key.GetAllMethods();
                var count = allMethods.Count(method => method.GetCustomAttributes(false).OfType<BenchmarkAttribute>()
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