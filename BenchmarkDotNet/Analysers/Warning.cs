using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Analysers
{
    public class Warning : IWarning
    {
        public string Kind { get; }
        public string Message { get; }
        public BenchmarkReport Report { get; }

        public Warning(string kind, string message, BenchmarkReport report)
        {
            Kind = kind;
            Message = message;
            Report = report;
        }
    }
}