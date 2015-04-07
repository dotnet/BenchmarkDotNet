namespace BenchmarkDotNet.Logging
{
    public interface IBenchmarkLogger
    {
        void Write(BenchmarkLogKind logKind, string format, params object[] args);
    }
}