namespace BenchmarkDotNet.Loggers
{
    public interface ILogger
    {
        string Id { get; }

        /// <summary>
        /// If there are several loggers with the same <see cref="Id"/>,
        /// only logger with the highest priority will be used.
        /// </summary>
        int Priority { get; }

        void Write(LogKind logKind, string text);

        void WriteLine();

        void WriteLine(LogKind logKind, string text);

        void Flush();
    }
}