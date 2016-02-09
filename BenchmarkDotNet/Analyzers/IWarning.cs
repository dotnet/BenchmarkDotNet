using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analyzers
{
    public interface IWarning
    {
        string Kind { get; }
        string Message { get; }
        BenchmarkReport Report { get; }
    }
}