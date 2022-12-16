using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.XUnit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class MonoTests : BenchmarkTestExecutor
    {
        [FactDotNetCoreOnly("UseMonoRuntime option is available in .NET Core only starting from .NET 6")]
        public void Mono70IsSupported()
        {
            if (ContinuousIntegration.IsAppVeyorOnWindows())
                return; // timeouts

            var config = ManualConfig.CreateEmpty().AddJob(Job.Dry.WithRuntime(MonoRuntime.Mono70));
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

                if (RuntimeInformation.GetCurrentRuntime() != MonoRuntime.Mono70)
                {
                    throw new Exception("Incorrect runtime detection");
                }
            }
        }
    }
}
