using System.Diagnostics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnosers
{
    // TODO: Refactoring
    public interface IDiagnoser
    {
        void Print(Benchmark benchmark, Process process, ILogger logger);
    }
}
