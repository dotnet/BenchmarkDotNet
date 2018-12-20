using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    public interface IIterationInfo
    {
        IterationMode IterationMode { get; }
        IterationStage IterationStage { get; }
    }
}