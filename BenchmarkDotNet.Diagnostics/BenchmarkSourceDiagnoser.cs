using System.Diagnostics;
using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Loggers;

namespace BenchmarkDotNet.Diagnostics
{
    public class BenchmarkSourceDiagnoser : BenchmarkDiagnoserBase, IBenchmarkDiagnoser
    {
        public string Name => "source";
        public string Description => "Source (IL/ASM) diagnoser";

        public void Print(Benchmark benchmark, Process process, string codeExeName, IBenchmarkLogger logger)
        {
            PrintCodeForMethod(benchmark, process, codeExeName, logger, true, true, false);
        }
    }
}