using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    internal class ValidationError : IValidationError
    {
        public ValidationError(bool isCritical, string message, Benchmark benchmark = null)
        {
            IsCritical = isCritical;
            Message = message;
            Benchmark = benchmark;
        }

        public bool IsCritical { get; }

        public string Message { get; }

        public Benchmark Benchmark { get; }

        public override string ToString() => Message;
    }
}