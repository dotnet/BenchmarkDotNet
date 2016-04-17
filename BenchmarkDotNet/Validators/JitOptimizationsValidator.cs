using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class JitOptimizationsValidator : IValidator
    {
        public static readonly IValidator DontFailOnError = new JitOptimizationsValidator(false);
        public static readonly IValidator FailOnError = new JitOptimizationsValidator(true);

        private JitOptimizationsValidator(bool failOnErrors)
        {
            TreatsWarningsAsErrors = failOnErrors;
        }

        public bool TreatsWarningsAsErrors { get; }

        public IEnumerable<IValidationError> Validate(IList<Benchmark> benchmarks)
        {
#if CORE
            yield break; // todo: implement when it becomes possible
#else
            foreach (var group in benchmarks.GroupBy(benchmark => benchmark.Target.Type.Assembly()))
            {
                foreach (var referencedAssemblyName in group.Key.GetReferencedAssemblies())
                {
                    var referencedAssembly = Assembly.Load(referencedAssemblyName);

                    if (IsJITOptimizationDisabled(referencedAssembly))
                    {
                        yield return new ValidationError(
                            TreatsWarningsAsErrors,
                            $"Assembly {group.Key} which defines benchmarks references non-optimized {referencedAssemblyName.Name}");
                    }
                }

                if (IsJITOptimizationDisabled(group.Key))
                {
                    yield return new ValidationError(
                        TreatsWarningsAsErrors,
                        $"Assembly {group.Key} which defines benchmarks is non-optimized");
                }
            }
#endif
        }

#if !CORE
        private bool IsJITOptimizationDisabled(Assembly assembly)
        {
            return assembly
                .GetCustomAttributes<DebuggableAttribute>(false)
                .Any(attribute => attribute.IsJITOptimizerDisabled);
        }
#endif
    }
}