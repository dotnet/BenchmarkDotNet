using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.IntegrationTests.DisabledOptimizations
{               
    internal class JitOptimizationsAnalyserConfig : ManualConfig
    {
        internal JitOptimizationsAnalyserConfig()
        {
            Add(Job.Dry);
            Add(Loggers.ConsoleLogger.Default);
            Add(Analysers.JitOptimizationsAnalyser.Instance);
        }
    }

    [Config(typeof(JitOptimizationsAnalyserConfig))]
    public class OptimizationsDisabledInCsproj
    {
        [Benchmark]
        public string Benchmark()
        {
            return "I have manually checked off 'optimize' in .csproj for RELEASE";
        }
    }
}
