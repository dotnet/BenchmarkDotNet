using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.IntegrationTests.DisabledOptimizations
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
    public class OptimizationsDisabledInCsproj
    {
        [Benchmark]
        public string Benchmark()
        {
            return "I have manually checked off 'optimize' in .csproj for RELEASE";
        }
    }
}
