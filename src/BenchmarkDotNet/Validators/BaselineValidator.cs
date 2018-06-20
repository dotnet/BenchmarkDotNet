using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Order;

namespace BenchmarkDotNet.Validators
{
    public class BaselineValidator : IValidator
    {
        public static readonly BaselineValidator FailOnError = new BaselineValidator();

        private BaselineValidator() { }

        public bool TreatsWarningsAsErrors => true; // it is a must!

        public IEnumerable<ValidationError> Validate(ValidationParameters input)
        {
            var allBenchmarks = input.Benchmarks.ToArray();
            var orderProvider = input.Config.GetOrderer() ?? DefaultOrderer.Instance;
            
            var benchmarkLogicalGroups = allBenchmarks
                .Select(benchmark => orderProvider.GetLogicalGroupKey(input.Config, allBenchmarks, benchmark))
                .ToArray();
            
            var logicalGroups = benchmarkLogicalGroups.Distinct().ToArray();
            foreach (var logicalGroup in logicalGroups)
            {
                var benchmarks = allBenchmarks.Where((benchmark, index) => benchmarkLogicalGroups[index] == logicalGroup).ToArray();
                var methodBaselineCount = benchmarks.Count(b => b.Descriptor.Baseline);
                var jobBaselineCount = benchmarks.Count(b => b.Job.Meta.Baseline);
                var className = benchmarks.First().Descriptor.Type.Name;

                if (methodBaselineCount > 1) 
                    yield return CreateError("benchmark method", "Baseline = true", logicalGroup, className, methodBaselineCount.ToString());

                if (jobBaselineCount > 1) 
                    yield return CreateError("job", "Baseline = true", logicalGroup, className, jobBaselineCount.ToString());

                if (methodBaselineCount > 0 && jobBaselineCount > 1)
                    yield return CreateError("job-benchmark pair", "Baseline property", logicalGroup, className, "both");
            }
        }

        private ValidationError CreateError(string subject, string property, string groupName, string className, string actual) => 
            new ValidationError(
                TreatsWarningsAsErrors, 
                $"Only 1 {subject} in a group can have \"{property}\" applied to it, group {groupName} in class {className} has {actual}");
    }
}