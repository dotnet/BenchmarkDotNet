using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Validators
{
    public class SetupCleanupValidator : IValidator
    {
        public static readonly SetupCleanupValidator FailOnError = new SetupCleanupValidator();

        private SetupCleanupValidator() { }

        public bool TreatsWarningsAsErrors => true; // it is a must!

        public IEnumerable<ValidationError> Validate(ValidationParameters input)
        {
            var validationErrors = new List<ValidationError>();

            foreach (var groupByType in input.Benchmarks.GroupBy(benchmark => benchmark.Descriptor.Type))
            {
                var allMethods = groupByType.Key.GetAllMethods().ToArray();

                validationErrors.AddRange(ValidateAttributes<GlobalSetupAttribute>(groupByType.Key.Name, allMethods));
                validationErrors.AddRange(ValidateAttributes<GlobalCleanupAttribute>(groupByType.Key.Name, allMethods));
                validationErrors.AddRange(ValidateAttributes<IterationSetupAttribute>(groupByType.Key.Name, allMethods));
                validationErrors.AddRange(ValidateAttributes<IterationSetupAttribute>(groupByType.Key.Name, allMethods));
            }

            return validationErrors;
        }

        private IEnumerable<ValidationError> ValidateAttributes<T>(string benchmarkClassName, IEnumerable<MethodInfo> allMethods) where T : TargetedAttribute
        {
            int emptyTargetCount = 0;
            var targetCount = new Dictionary<string, int>();

            foreach (var method in allMethods)
            {
                var attributes = method.GetCustomAttributes(false).OfType<T>();

                foreach (var attribute in attributes)
                {
                    if (attribute.Targets.IsNullOrEmpty())
                    {
                        emptyTargetCount++;
                    }
                    else
                    {
                        foreach (string target in attribute.Targets)
                        {
                            if (!targetCount.ContainsKey(target))
                            {
                                targetCount[target] = 1;
                            }
                            else
                            {
                                targetCount[target] += 1;
                            }
                        }
                    }
                }
            }

            if (emptyTargetCount > 1)
            {
                yield return new ValidationError(
                    TreatsWarningsAsErrors,
                    $"Only 1 [{typeof(T).Name}] in a class can have an empty target applied to it, class {benchmarkClassName} has {emptyTargetCount}");
            }

            foreach (var targetPair in targetCount)
            {
                if (targetPair.Value > 1)
                {
                    yield return new ValidationError(
                        TreatsWarningsAsErrors,
                        $"Only 1 [{typeof(T).Name}] in a class can \"Target = {targetPair.Key}\" applied to it, class {benchmarkClassName} has {targetPair.Value}");
                }
            }
        }
    }
}
