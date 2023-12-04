using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
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
#if NET461
            // We cannot detect what target framework version the host was compiled for on full Framework,
            // which causes the RoslynToolchain to be used instead of CsProjClassicNetToolchain when the host is full Framework
            // (because full Framework always uses the version that's installed on the machine, unlike Core),
            // which means if the machine has net48 installed (not net481), the net461 host with net48 runtime moniker
            // will not be recompiled, causing the test to fail.

            // If we ever change the default toolchain to CsProjClassicNetToolchain instead of RoslynToolchain, we can remove this check.
            if (runtime == RuntimeMoniker.Net48)
            {
                // XUnit doesn't provide Assert.Skip API yet.
                return;
            }
#endif
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