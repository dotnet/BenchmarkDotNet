using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Extensions;

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

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            foreach (var group in validationParameters.Benchmarks.GroupBy(benchmark => benchmark.Descriptor.Type.GetTypeInfo().Assembly))
            {
                foreach (var referencedAssemblyName in group.Key.GetReferencedAssemblies())
                {
                    var referencedAssembly = Assembly.Load(referencedAssemblyName);

                    // LINQPad.exe is non-optimized on purpose, see https://github.com/dotnet/BenchmarkDotNet/issues/580#issuecomment-345484889 for more details
                    // we don't warn about non-optimized dependency to LINQPad
                    // but we give extra hint if the dll with benchmark itself was build without optimization by LINQPad
                    if (referencedAssembly.IsJitOptimizationDisabled().IsTrue() && !referencedAssembly.IsLinqPad())
                    {
                        yield return new ValidationError(
                            TreatsWarningsAsErrors,
                            $"Assembly {group.Key.GetName().Name} which defines benchmarks references non-optimized {referencedAssemblyName.Name}"
                            + (TreatsWarningsAsErrors
                                ? $"{Environment.NewLine}\tIf you own this dependency, please, build it in RELEASE." +
                                  $"{Environment.NewLine}\tIf you don't, you can create custom config with {nameof(DontFailOnError)} to disable our custom policy and allow this benchmark to run."
                                : string.Empty));
                    }
                }

                if (group.Key.IsJitOptimizationDisabled().IsTrue())
                {
                    yield return new ValidationError(
                        TreatsWarningsAsErrors,
                        $"Assembly {group.Key.GetName().Name} which defines benchmarks is non-optimized" + Environment.NewLine +
                        "Benchmark was built without optimization enabled (most probably a DEBUG configuration). Please, build it in RELEASE."
                        + (group.Key.IsLinqPad()
                            ? Environment.NewLine + "Please enable optimizations in your LINQPad. Go to Preferences -> Query and select \"compile with /optimize+\""
                            : string.Empty));
                }
            }
        }        
    }
}