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
            AddJob(Job.Dry);
            AddLogger(Loggers.ConsoleLogger.Default);
            AddValidator(JitOptimizationsValidator.DontFailOnError);
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
