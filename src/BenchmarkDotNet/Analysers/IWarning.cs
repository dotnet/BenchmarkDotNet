using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analysers
{
    public interface IWarning
    {
        string Kind { get; }
        string Message { get; }
        BenchmarkReport Report { get; }
    }
}