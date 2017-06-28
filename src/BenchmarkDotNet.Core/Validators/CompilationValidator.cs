using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class CompilationValidator : IValidator
    {
        private static readonly HashSet<char> ValidConnectors = new HashSet<char> { '-', '_' };

        public static readonly IValidator Default = new CompilationValidator();

        private CompilationValidator() { }

        public bool TreatsWarningsAsErrors => true;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => validationParameters
                .Benchmarks
                .Where(benchmark => !IsValidCSharpIdentifier(benchmark.Target.Method.Name))
                .Distinct(BenchmarkMethodEqualityComparer.Default) // we might have multiple jobs targeting same method. Single error should be enough ;)
                .Select(benchmark
                    => new ValidationError(
                        true,
                        $"Benchmarked method `{benchmark.Target.Method.Name}` contains illegal characters (whitespaces). Please use `[<Benchmark(Description = \"Custom name\")>]` to set custom display name.",
                        benchmark
                    ));

        private bool IsValidCSharpIdentifier(string identifier) // F# allows to use whitespaces as names #479
            => !string.IsNullOrEmpty(identifier)
               && (char.IsLetter(identifier[0]) || identifier[0] == '_') // An identifier must start with a letter or an underscore
               && identifier
                    .Skip(1)
                    .All(character => char.IsLetterOrDigit(character) || ValidConnectors.Contains(character));

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