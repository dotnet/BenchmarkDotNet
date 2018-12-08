using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    public struct StoppingResolution
    {
        public readonly bool IsFinished;

        [CanBeNull]
        public readonly string Message;

        private StoppingResolution(bool isFinished, [CanBeNull] string message)
        {
            IsFinished = isFinished;
            Message = message;
        }

        public static readonly StoppingResolution NotFinished = new StoppingResolution(false, null);
        public static StoppingResolution CreateFinished([NotNull] string message) => new StoppingResolution(true, message);
    }
}