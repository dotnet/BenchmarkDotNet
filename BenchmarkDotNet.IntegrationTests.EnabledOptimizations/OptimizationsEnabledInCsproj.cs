using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.IntegrationTests.EnabledOptimizations
{
    internal class JitOptimizationsValidatorConfig : ManualConfig
    {
        public JitOptimizationsValidatorConfig()
        {
            Add(Job.Dry);
            Add(Loggers.ConsoleLogger.Default);
            Add(JitOptimizationsValidator.DontFailOnError);
        }
    }

    [Config(typeof(JitOptimizationsValidatorConfig))]
    public class OptimizationsEnabledInCsproj
    {
        [Benchmark]
        public string Benchmark()
        {
            return "I have manually checked 'optimize' in .csproj for DEBUG";
        }
    }
}