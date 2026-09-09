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
                // An unreadable type is reported here as well as by TypeFilter: BenchmarkRunner.Run<T>() and both
                // test adapters never go through TypeFilter, so for them this is the only report there is.
                .Where(built => !built.IsSuccess)
                .Select(built => new ValidationError(false, built.Error!))
                .ToAsyncEnumerable();
    }
}