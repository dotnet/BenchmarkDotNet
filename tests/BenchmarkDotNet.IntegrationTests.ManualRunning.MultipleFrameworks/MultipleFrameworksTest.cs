using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class MultipleFrameworksTest : BenchmarkTestExecutor
    {
        private const string TfmEnvVarName = "TfmEnvVarName";

        [Theory]
        [InlineData(RuntimeMoniker.Net461)]
        [InlineData(RuntimeMoniker.Net48)]
        [InlineData(RuntimeMoniker.NetCoreApp20)]
        [InlineData(RuntimeMoniker.Net70)]
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
#elif NET7_0
                RuntimeMoniker.Net70;
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