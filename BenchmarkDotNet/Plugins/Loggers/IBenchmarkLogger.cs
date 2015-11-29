namespace BenchmarkDotNet.Plugins.Loggers
{
    public interface IBenchmarkLogger : IPlugin
    {
        void Write(BenchmarkLogKind logKind, string format, params object[] args);
    }
}