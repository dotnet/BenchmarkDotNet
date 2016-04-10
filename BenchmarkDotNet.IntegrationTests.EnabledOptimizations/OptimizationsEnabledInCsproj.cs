using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.IntegrationTests.EnabledOptimizations
{
    internal class JitOptimizationsAnalyserConfig : ManualConfig
    {
        internal JitOptimizationsAnalyserConfig()
        {
            Add(Job.Dry);
            Add(Loggers.ConsoleLogger.Default);
            Add(Analyzers.JitOptimizationsAnalyser.Instance);
        }
    }

    [Config(typeof(JitOptimizationsAnalyserConfig))]
    public class OptimizationsEnabledInCsproj
    {
        [Benchmark]
        public string Benchmark()
        {
            return "I have manually checked 'optimize' in .csproj for DEBUG";
        }
    }
}