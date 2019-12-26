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
            AddJob(Job.Dry);
            AddLogger(Loggers.ConsoleLogger.Default);
            AddValidator(JitOptimizationsValidator.DontFailOnError);
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