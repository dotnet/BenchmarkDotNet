using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class CompilationValidator : IValidator
    {
        public static readonly IValidator Default = new CompilationValidator();

        private CompilationValidator() { }

        public bool TreatsWarningsAsErrors => true;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => validationParameters
                .Benchmarks
                .Where(benchmark => HasInvalidCSharpName(benchmark.Target.Method))
                .Distinct(BenchmarkMethodEqualityComparer.Default) // we might have multiple jobs targeting same method. Single error should be enough ;)
                .Select(benchmark
                    => new ValidationError(
                        true,
                        $"Benchmarked method `{benchmark.Target.Method.Name}` contains illegal characters (whitespaces). Please use `[<Benchmark(Description = \"Custom name\")>]` to set custom display name.",
                        benchmark
                    ));

        private bool HasInvalidCSharpName(MethodInfo targetMethod)
            => targetMethod.Name.Any(char.IsWhiteSpace); // F# allows to use whitespaces as names #479

        private class BenchmarkMethodEqualityComparer : IEqualityComparer<Benchmark>
        {
            internal static readonly IEqualityComparer<Benchmark> Default = new BenchmarkMethodEqualityComparer();

            public bool Equals(Benchmark x, Benchmark y)
                => x.Target.Method.Equals(y.Target.Method);

            public int GetHashCode(Benchmark obj)
                => obj.Target.Method.GetHashCode();
        }
    }
}