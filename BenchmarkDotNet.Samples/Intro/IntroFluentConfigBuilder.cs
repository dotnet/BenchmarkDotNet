using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Samples.Intro
{
    public class IntroFluentConfigBuilder
    {
        public static void Run()
        {
            ManualConfig
                .Create(DefaultConfig.Instance)
                .With(Job.RyuJitX64)
                .With(Job.Core)
                .With(ExecutionValidator.FailOnError);
        }
    }
}