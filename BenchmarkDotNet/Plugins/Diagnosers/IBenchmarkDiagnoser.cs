using System.Diagnostics;
using BenchmarkDotNet.Plugins.Loggers;

namespace BenchmarkDotNet.Plugins.Diagnosers
{
    /// <summary>
    /// This is the interface that we expect the BenchmarkDotNet.Diagnostics "plugin" to implement
    /// </summary>
    public interface IBenchmarkDiagnoser : IPlugin
    {
        void Print(Benchmark benchmark, Process process, IBenchmarkLogger logger);
    }
}
