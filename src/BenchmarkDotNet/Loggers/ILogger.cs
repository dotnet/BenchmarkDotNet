namespace BenchmarkDotNet.Loggers
{
    public interface ILogger
    {
        void Write(LogKind logKind, string text);

        void WriteLine();

        void WriteLine(LogKind logKind, string text);
    }
}