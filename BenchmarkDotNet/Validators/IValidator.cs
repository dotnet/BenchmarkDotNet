using System.Collections.Generic;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public interface IValidator
    {
        bool TreatsWarningsAsErrors { get; }

        IEnumerable<ValidationError> Validate(IList<Benchmark> benchmarks);
    }
}