namespace BenchmarkDotNet.Plugins.Loggers
{
    public interface IBenchmarkLogger
    {
        void Write(BenchmarkLogKind logKind, string format, params object[] args);
    }
}