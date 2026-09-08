using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Validators
{
    public class GenericBenchmarksValidator : IValidator
    {
        public static readonly IValidator DontFailOnError = new GenericBenchmarksValidator();

        public bool TreatsWarningsAsErrors => false;

        public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters)
            => validationParameters
                .Benchmarks
                .Select(benchmark => benchmark.Descriptor.Type.Assembly)
                .Distinct()
                .SelectMany(assembly => assembly.GetRunnableBenchmarks())
                .SelectMany(GenericBenchmarksBuilder.BuildGenericsIfNeeded)
                // An unreadable type is reported by TypeFilter, which sees it even when nothing else survives.
                .Where(built => !built.IsSuccess && !built.IsUnreadable)
                .Select(built => new ValidationError(false, built.Error!))
                .ToAsyncEnumerable();
    }
}