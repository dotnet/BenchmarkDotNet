using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.Mono;

namespace BenchmarkDotNet.IntegrationTests
{
    public class MonoTests : BenchmarkTestExecutor
    {
        [FactDotNetCoreOnly("UseMonoRuntime option is available in .NET Core only starting from .NET 6")]
        public void Mono60IsSupported()
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry
                    .WithRuntime(MonoRuntime.Mono60)
                    .WithToolchain(MonoToolchain.From(new NetCoreAppSettings("net6.0", null, "mono60"))));
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
            }
        }
    }
}
