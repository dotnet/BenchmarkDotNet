using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace BenchmarkDotNet.Validators
{
    [UsedImplicitly]
    public static class BenchmarkProcessValidator
    {
        [UnconditionalSuppressMessage(category: "Trimming", checkId: "IL2075",
            Justification = "The generated runnable derives from the benchmark type and calls its [Benchmark] methods directly, so they are kept.")]
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