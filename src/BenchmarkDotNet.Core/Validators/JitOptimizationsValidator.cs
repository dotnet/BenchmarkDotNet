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
            foreach (var group in validationParameters.Benchmarks.GroupBy(benchmark => benchmark.Target.Type.GetTypeInfo().Assembly))
            {
                foreach (var referencedAssemblyName in group.Key.GetReferencedAssemblies())
                {
                    var referencedAssembly = Assembly.Load(referencedAssemblyName);

                    if (referencedAssembly.IsJitOptimizationDisabled().IsTrue())
                    {
                        yield return new ValidationError(
                            TreatsWarningsAsErrors,
                            $"Assembly {group.Key.GetName().Name} which defines benchmarks references non-optimized {referencedAssemblyName.Name}");
                    }
                }

                if (group.Key.IsJitOptimizationDisabled().IsTrue())
                {
                    yield return new ValidationError(
                        TreatsWarningsAsErrors,
                        $"Assembly {group.Key.GetName().Name} which defines benchmarks is non-optimized");
                }
            }
        }
    }
}