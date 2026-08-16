using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests;

namespace BenchmarkDotNet.IntegrationTests.ManualRunning
{
    public class MultipleFrameworksTest : BenchmarkTestExecutor
    {
        private const string TfmEnvVarName = "TfmEnvVarName";

        [Test]
        [TUnit.Core.Arguments(RuntimeMoniker.Net462)]
        [TUnit.Core.Arguments(RuntimeMoniker.Net48)]
        [TUnit.Core.Arguments(RuntimeMoniker.Net80)]
        [TUnit.Core.Arguments(RuntimeMoniker.Net10_0)]
        public void EachFrameworkIsRebuilt(RuntimeMoniker runtime)
        {
            var config = ManualConfig.CreateEmpty().AddJob(Job.Dry.WithRuntime(runtime.GetRuntime()).WithEnvironmentVariable(TfmEnvVarName, runtime.ToString()));
            CanExecute<ValuePerTfm>(config);
        }

        public class ValuePerTfm
        {
            private const RuntimeMoniker moniker =
#if NET462
                RuntimeMoniker.Net462;
#elif NET48
                RuntimeMoniker.Net48;
#elif NET8_0
                RuntimeMoniker.Net80;
#elif NET10_0
                RuntimeMoniker.Net10_0;
#else
                RuntimeMoniker.NotRecognized;
#endif

            [Benchmark]
            public void ThrowWhenWrong()
            {
                if (Environment.GetEnvironmentVariable(TfmEnvVarName) != moniker.ToString())
                {
                    throw new InvalidOperationException($"Has not been recompiled, the value was {moniker}, expected {Environment.GetEnvironmentVariable(TfmEnvVarName)}");
                }
            }
        }
    }
}
