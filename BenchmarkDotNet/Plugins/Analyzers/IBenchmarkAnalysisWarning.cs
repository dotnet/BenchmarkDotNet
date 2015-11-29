using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Analyzers
{
    public interface IBenchmarkAnalysisWarning
    {
        string Kind { get; }
        string Message { get; }
        BenchmarkReport Report { get; }
    }
}