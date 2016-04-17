using System.Collections.Generic;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public interface IValidator
    {
        IEnumerable<IValidationError> Validate(IList<Benchmark> benchmarks);
    }
}