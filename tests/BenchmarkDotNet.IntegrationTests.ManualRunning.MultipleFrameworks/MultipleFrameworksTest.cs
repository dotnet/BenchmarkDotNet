using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests.ManualRunning
{
    // Note: To properly test this locally, modify
    // BenchmarkDotNet.IntegrationTests.ManualRunning.MultipleFrameworks.csproj,
    // following the comments in that file.
    public class MultipleFrameworksTest : BenchmarkTestExecutor
    {
        private const string TfmEnvVarName = "TfmEnvVarName";

        [Theory]
        [InlineData(RuntimeMoniker.Net461)]
        [InlineData(RuntimeMoniker.Net48)]
        [InlineData(RuntimeMoniker.NetCoreApp20)]
        [InlineData(RuntimeMoniker.Net80)]
        public void EachFrameworkIsRebuilt(RuntimeMoniker runtime)
        {
            var config = ManualConfig.CreateEmpty().AddJob(Job.Dry.WithRuntime(runtime.GetRuntime()).WithEnvironmentVariable(TfmEnvVarName, runtime.ToString()));
            CanExecute<ValuePerTfm>(config);
        }

        public class ValuePerTfm
        {
            private const RuntimeMoniker moniker =
#if NET461
                RuntimeMoniker.Net461;
#elif NET48
                RuntimeMoniker.Net48;
#elif NETCOREAPP2_0
                RuntimeMoniker.NetCoreApp20;
#elif NET8_0
                RuntimeMoniker.Net80;
#else
                RuntimeMoniker.NotRecognized;
#endif

            [Benchmark]
            public void ThrowWhenWrong()
            {
                if (Environment.GetEnvironmentVariable(TfmEnvVarName) != moniker.ToString())
                {
                    throw new InvalidOperationException($"Has not been recompiled, the value was {Environment.GetEnvironmentVariable(TfmEnvVarName)}");
                }
            }
        }
    }
}