using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Samples.Algorithms;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Samples.Intro
{
    public class IntroFluentConfigBuilder
    {
        public static void Run()
        {
            BenchmarkRunner
                .Run<Algo_Md5VsSha256>(
                    ManualConfig
                        .Create(DefaultConfig.Instance)
                        .With(Job.RyuJitX64)
                        .With(Job.Core)
                        .With(ExecutionValidator.FailOnError));
        }
    }
}