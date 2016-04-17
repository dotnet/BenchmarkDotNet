using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public interface IValidationError
    {
        bool IsCritical { get; }

        string Message { get; } 

        Benchmark Benchmark { get; }
    }
}