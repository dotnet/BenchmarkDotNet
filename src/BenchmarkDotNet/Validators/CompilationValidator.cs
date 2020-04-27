using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using Microsoft.CodeAnalysis.CSharp;

namespace BenchmarkDotNet.Validators
{
    public class CompilationValidator : IValidator
    {
        private const char Underscore = '_';

        public static readonly IValidator FailOnError = new CompilationValidator();

        private static readonly ImmutableHashSet<string> CsharpKeywords = GetCsharpKeywords().ToImmutableHashSet();

        private CompilationValidator() { }

        public bool TreatsWarningsAsErrors => true;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => ValidateCSharpNaming(validationParameters.Benchmarks)
                    .Union(ValidateNamingConflicts(validationParameters.Benchmarks))
                    .Union(ValidateAccessModifiers(validationParameters.Benchmarks))
                    .Union(ValidateBindingModifiers(validationParameters.Benchmarks));

        private static IEnumerable<ValidationError> ValidateCSharpNaming(IEnumerable<BenchmarkCase> benchmarks)
            => benchmarks
                .Where(benchmark => !IsValidCSharpIdentifier(benchmark.Descriptor.WorkloadMethod.Name))
                .Distinct(BenchmarkMethodEqualityComparer.Instance) // we might have multiple jobs targeting same method. Single error should be enough ;)
                .Select(benchmark
                    => new ValidationError(
                        true,
                        $"Benchmarked method `{benchmark.Descriptor.WorkloadMethod.Name}` contains illegal character(s) or uses C# keyword. Please use `[<Benchmark(Description = \"Custom name\")>]` to set custom display name.",
                        benchmark
                    ));

        private static IEnumerable<ValidationError> ValidateNamingConflicts(IEnumerable<BenchmarkCase> benchmarks)
            => benchmarks
                .Select(benchmark => benchmark.Descriptor.Type)
                .Distinct()
                .Where(type => type.GetAllMethods().Any(method => IsUsingNameUsedInternallyByOurTemplate(method.Name)))
                .Select(benchmark
                    => new ValidationError(
                        true,
                        "Using \"__Overhead\" for method name is prohibited. We are using it internally in our templates. Please rename your method"));

        private static IEnumerable<ValidationError> ValidateAccessModifiers(IEnumerable<BenchmarkCase> benchmarks)
            => benchmarks.Where(x => x.Descriptor.Type.IsGenericType
                                     && HasPrivateGenericArguments(x.Descriptor.Type))
                         .Select(benchmark => new ValidationError(true, $"Generic class {benchmark.Descriptor.Type.GetDisplayName()} has non public generic argument(s)"));

        private static IEnumerable<ValidationError> ValidateBindingModifiers(IEnumerable<BenchmarkCase> benchmarks)
            => benchmarks.Where(x => x.Descriptor.WorkloadMethod.IsStatic && !x.GetToolchain().IsInProcess)
                          .Distinct(BenchmarkMethodEqualityComparer.Instance)
                          .Select(benchmark
                              => new ValidationError(
                                  true,
                                  $"Benchmarked method `{benchmark.Descriptor.WorkloadMethod.Name}` is static. Benchmarks MUST be instance methods, static methods are not supported.",
                                  benchmark
                              ));

        private static bool IsValidCSharpIdentifier(string identifier) // F# allows to use whitespaces as names #479
            => !string.IsNullOrEmpty(identifier)
               && (char.IsLetter(identifier[0]) || identifier[0] == Underscore) // An identifier must start with a letter or an underscore
               && identifier.Skip(1).All(character => char.IsLetterOrDigit(character) || character == Underscore)
               && !CsharpKeywords.Contains(identifier);

        private static bool IsUsingNameUsedInternallyByOurTemplate(string identifier)
            => identifier == "__Overhead";

        private static bool HasPrivateGenericArguments(Type type) => type.GetGenericArguments().Any(a => !(a.IsPublic || a.IsNestedPublic));

        // source: https://stackoverflow.com/a/19562316
        private static IEnumerable<string> GetCsharpKeywords()
        {
            var memberInfos = typeof(SyntaxKind).GetMembers(BindingFlags.Public | BindingFlags.Static);

            return from memberInfo in memberInfos
                           where memberInfo.Name.EndsWith("Keyword")
                           orderby memberInfo.Name
                           select memberInfo.Name.Substring(startIndex: 0, length: memberInfo.Name.IndexOf("Keyword", StringComparison.Ordinal)).ToLower();
        }

        private class BenchmarkMethodEqualityComparer : IEqualityComparer<BenchmarkCase>
        {
            internal static readonly IEqualityComparer<BenchmarkCase> Instance = new BenchmarkMethodEqualityComparer();

            public bool Equals(BenchmarkCase x, BenchmarkCase y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;
                if (x.Descriptor.WorkloadMethod.Equals(y.Descriptor.WorkloadMethod))
                    return true;
                return false;
            }

            public int GetHashCode(BenchmarkCase obj) => obj.Descriptor.WorkloadMethod.GetHashCode();
        }
    }
}