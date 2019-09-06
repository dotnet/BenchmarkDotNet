using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class JitRuntimeValidationTest
    {
        protected readonly ITestOutputHelper Output;

        private class PlatformConfig : ManualConfig
        {
            public PlatformConfig(Runtime runtime, Jit jit, Platform platform)
            {
                Add(new Job(Job.Dry, new EnvironmentMode()
                {
                    Runtime = runtime,
                    Jit = jit,
                    Platform = platform
                }));
            }
        }

        private const string OkCaption = "// OkCaption";
        private const string LegacyJitNotAvailableForMono = "// ERROR:  LegacyJIT is requested but it is not available for Mono";
        private const string RyuJitNotAvailable = "// ERROR:  RyuJIT is requested but it is not available in current environment";
        private const string ToolchainSupportsOnlyRyuJit = "Currently dotnet cli toolchain supports only RyuJit";

        public JitRuntimeValidationTest(ITestOutputHelper outputHelper)
        {
            Output = outputHelper;
        }

        [TheoryWindowsOnly("CLR is a valid job only on Windows")]
        [InlineData(Jit.LegacyJit, Platform.X86, OkCaption)]
        [InlineData(Jit.LegacyJit, Platform.X64, OkCaption)]
        [InlineData(Jit.RyuJit, Platform.X86, RyuJitNotAvailable)]
        [InlineData(Jit.RyuJit, Platform.X64, OkCaption)]
        public void CheckClrOnWindows(Jit jit, Platform platform, string expectedText)
        {
            Verify(ClrRuntime.Net461, jit, platform, expectedText);
        }
        
//        [TheoryWindowsOnly("CLR is a valid job only on Windows")]
//        [InlineData(Jit.LegacyJit, Platform.X86, LegacyJitNotAvailableForMono)]
//        [InlineData(Jit.LegacyJit, Platform.X64, LegacyJitNotAvailableForMono)]
//        [InlineData(Jit.RyuJit, Platform.X86, RyuJitNotAvailable)]
//        [InlineData(Jit.RyuJit, Platform.X64, RyuJitNotAvailable)]
//        public void CheckMono(Jit jit, Platform platform, string expectedText)
//        {
//            Verify(Runtime.Mono, jit, platform, expectedText);
//        }

        [Theory]
        [InlineData(Jit.LegacyJit, Platform.X86, ToolchainSupportsOnlyRyuJit)]
        [InlineData(Jit.LegacyJit, Platform.X64, ToolchainSupportsOnlyRyuJit)]
        [InlineData(Jit.RyuJit, Platform.X64, OkCaption)]
        public void CheckCore(Jit jit, Platform platform, string expectedText)
        {
            Verify(CoreRuntime.Core21, jit, platform, expectedText);
        }

        private void Verify(Runtime runtime, Jit jit, Platform platform, string expectedText)
        {
            var logger = new OutputLogger(Output);
            var config = new PlatformConfig(runtime, jit, platform).With(logger).With(DefaultColumnProviders.Instance);

            BenchmarkRunner.Run(new[] { BenchmarkConverter.TypeToBenchmarks(typeof(TestBenchmark), config) });

            Assert.Contains(expectedText, logger.GetLog());
        }

        public class TestBenchmark
        {
            [Benchmark]
            public void Benchmark()
            {
                Console.WriteLine(OkCaption);
            }
        }
    }
}