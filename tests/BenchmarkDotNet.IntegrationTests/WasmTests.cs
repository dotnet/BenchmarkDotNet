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
    public class WasmTests : BenchmarkTestExecutor
    {
        public WasmTests(ITestOutputHelper output) : base(output) { }

        [FactEnvSpecific("WASM is only supported on Unix", EnvRequirement.NonWindows)]
        public void WasmIsSupported()
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty()
                .AddLogger(logger)
                .AddJob(Job.Dry.WithRuntime(WasmRuntime.Default));
            CanExecute<WasmBenchmark>(config);
        }

        public class WasmBenchmark
        {
            [Benchmark]
            public void Check()
            {
                if (RuntimeInformation.GetCurrentRuntime() != WasmRuntime.Default)
                {
                    throw new Exception("Incorrect runtime detection");
                }
            }
        }
    }
}