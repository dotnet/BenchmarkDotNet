using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Validators
{
    public class GenericBenchmarksValidator : IValidator
    {
        public static readonly IValidator DontFailOnError = new GenericBenchmarksValidator();
        
        public bool TreatsWarningsAsErrors => false;
        
        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => validationParameters
                .Benchmarks
                .Select(benchmark => benchmark.Target.Type.Assembly)
                .Distinct()
                .SelectMany(assembly => assembly.GetRunnableBenchmarks())
                .SelectMany(GenericBenchmarksBuilder.BuildGenericsIfNeeded)
                .Where(result => !result.isSuccess)
                .Select(result => new ValidationError(false, $"Genetic type {result.result.Name} failed to build due to wrong type argument or arguments count, ignoring."));
    }
}