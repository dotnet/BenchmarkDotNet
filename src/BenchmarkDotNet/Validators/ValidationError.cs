using BenchmarkDotNet.Running;
using JetBrains.Annotations;

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

        [PublicAPI] public bool IsCritical { get; }
        [PublicAPI] public string Message { get; }
        [PublicAPI] public BenchmarkCase BenchmarkCase { get; }
        
        public override string ToString() => Message;
    }
}