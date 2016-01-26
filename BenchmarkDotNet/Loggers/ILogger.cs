namespace BenchmarkDotNet.Loggers
{
    public interface ILogger
    {
        void Write(LogKind logKind, string format, params object[] args);
    }
}