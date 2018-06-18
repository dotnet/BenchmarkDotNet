using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class ValidationError
    {
        public ValidationError(bool isCritical, string message, BenchmarkCase benchmarkCase = null)
        {
            IsCritical = isCritical;
            Message = message;
            BenchmarkCase = benchmarkCase;
        }

        public bool IsCritical { get; }

        public string Message { get; }

        public BenchmarkCase BenchmarkCase { get; }

        public override string ToString() => Message;
    }
}