using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    public struct StoppingResult
    {
        public readonly bool IsFinished;

        [CanBeNull]
        public readonly string Message;

        private StoppingResult(bool isFinished, [CanBeNull] string message)
        {
            IsFinished = isFinished;
            Message = message;
        }

        public static readonly StoppingResult NotFinished = new StoppingResult(false, null);
        public static StoppingResult CreateFinished([NotNull] string message) => new StoppingResult(true, message);
    }
}