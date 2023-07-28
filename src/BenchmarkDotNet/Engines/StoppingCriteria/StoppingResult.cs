namespace BenchmarkDotNet.Engines
{
    public struct StoppingResult
    {
        public readonly bool IsFinished;

        public readonly string? Message;

        private StoppingResult(bool isFinished, string? message)
        {
            IsFinished = isFinished;
            Message = message;
        }

        public static readonly StoppingResult NotFinished = new StoppingResult(false, null);
        public static StoppingResult CreateFinished(string message) => new StoppingResult(true, message);
    }
}