using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Analyzers
{
    public class BenchmarkAnalysisWarning : IBenchmarkAnalysisWarning
    {
        public string Kind { get; }
        public string Message { get; }
        public BenchmarkReport Report { get; }

        public BenchmarkAnalysisWarning(string kind, string message, BenchmarkReport report)
        {
            Kind = kind;
            Message = message;
            Report = report;
        }
    }
}