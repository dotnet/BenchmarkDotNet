using System.Collections.Generic;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public interface IValidator
    {
        bool TreatsWarningsAsErrors { get; }

        IEnumerable<IValidationError> Validate(IList<Benchmark> benchmarks);
    }
}