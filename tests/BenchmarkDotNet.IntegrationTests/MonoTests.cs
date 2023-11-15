using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class MonoTests : BenchmarkTestExecutor
    {
        public MonoTests(ITestOutputHelper output) : base(output) { }

        [FactEnvSpecific("UseMonoRuntime option is available in .NET Core only starting from .NET 6", EnvRequirement.DotNetCoreOnly)]
        public void Mono80IsSupported()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty()
                .AddLogger(logger)
                .AddJob(Job.Dry.WithRuntime(MonoRuntime.Mono80));
            CanExecute<MonoBenchmark>(config);
        }

        public class MonoBenchmark
        {
            [Benchmark]
            public void Check()
            {
                if (Type.GetType("Mono.RuntimeStructs") == null)
                {
                    throw new Exception("This is not Mono runtime");
                }

                if (RuntimeInformation.GetCurrentRuntime() != MonoRuntime.Mono80)
                {
                    throw new Exception("Incorrect runtime detection");
                }
            }
        }
    }
}