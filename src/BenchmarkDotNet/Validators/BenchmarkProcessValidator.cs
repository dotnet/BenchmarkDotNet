using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet.Validators
{
    [UsedImplicitly]
    public static class BenchmarkProcessValidator
    {
        public static IEnumerable<ValidationError> Validate(Job job, object benchmarkInstance)
        {
            foreach (var validationError in BenchmarkEnvironmentInfo.Validate(job))
            {
                yield return validationError;
            }

            var inlineableBenchmarks = benchmarkInstance.GetType().BaseType!
                .GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.HasAttribute<BenchmarkAttribute>() && !method.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining));
            foreach (var benchmark in inlineableBenchmarks)
            {
                yield return new ValidationError(
                    true,
                    $"Benchmark method `{benchmark.Name}` does not have MethodImplOptions.NoInlining flag set." +
                    $" You may need to rebuild your project, or apply it manually if you are not using MSBuild to build your project."
                );
            }
        }
    }
}