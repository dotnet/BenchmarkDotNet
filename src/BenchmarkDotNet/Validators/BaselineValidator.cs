using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Validators
{
    public class BaselineValidator : IValidator
    {
        public static readonly BaselineValidator FailOnError = new BaselineValidator();

        private BaselineValidator() { }

        public bool TreatsWarningsAsErrors => true; // it is a must!

        public IEnumerable<ValidationError> Validate(ValidationParameters input)
        {
            var allBenchmarks = input.Benchmarks.ToImmutableArray();
            var orderProvider = input.Config.Orderer;

            var benchmarkLogicalGroups = allBenchmarks
                .Select(benchmark => orderProvider.GetLogicalGroupKey(allBenchmarks, benchmark))
                .ToArray();

            var logicalGroups = benchmarkLogicalGroups.Distinct().ToArray();
            foreach (string logicalGroup in logicalGroups)
            {
                var benchmarks = allBenchmarks.Where((benchmark, index) => benchmarkLogicalGroups[index] == logicalGroup).ToArray();
                int methodBaselineCount = benchmarks.Select(b => b.Descriptor).Distinct().Count(it => it.Baseline);
                int jobBaselineCount = benchmarks.Select(b => b.Job).Distinct(JobComparer.Instance).Count(it => it.Meta.Baseline);
                string className = benchmarks.First().Descriptor.Type.Name;

                if (methodBaselineCount > 1)
                    yield return CreateError("benchmark method", "Baseline = true", logicalGroup, className, methodBaselineCount.ToString());

                if (jobBaselineCount > 1)
                    yield return CreateError("job", "Baseline = true", logicalGroup, className, jobBaselineCount.ToString());
            }
        }

        private ValidationError CreateError(string subject, string property, string groupName, string className, string actual) =>
            new ValidationError(
                TreatsWarningsAsErrors,
                $"Only 1 {subject} in a group can have \"{property}\" applied to it, group {groupName} in class {className} has {actual}");
    }
}