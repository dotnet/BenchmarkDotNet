﻿using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class JitRuntimeValidationTest : BenchmarkTestExecutor
    {
        public JitRuntimeValidationTest(ITestOutputHelper output) : base(output) { }

//      private const string LegacyJitNotAvailableForMono = "// ERROR:  LegacyJIT is requested but it is not available for Mono";
        private const string RyuJitNotAvailable = "// ERROR:  RyuJIT is requested but it is not available in current environment";
        private const string ToolchainSupportsOnlyRyuJit = "Currently dotnet cli toolchain supports only RyuJit";

        [TheoryWindowsOnly("CLR is a valid job only on Windows")]
        [InlineData(Jit.LegacyJit, Platform.X86, null)]
        [InlineData(Jit.LegacyJit, Platform.X64, null)]
        [InlineData(Jit.RyuJit, Platform.X86, RyuJitNotAvailable)]
        [InlineData(Jit.RyuJit, Platform.X64, null)]
        public void CheckClrOnWindows(Jit jit, Platform platform, string errorMessage)
        {
            Verify(ClrRuntime.Net462, jit, platform, errorMessage);
        }

//      [TheoryWindowsOnly("CLR is a valid job only on Windows")]
//      [InlineData(Jit.LegacyJit, Platform.X86, LegacyJitNotAvailableForMono)]
//      [InlineData(Jit.LegacyJit, Platform.X64, LegacyJitNotAvailableForMono)]
//      [InlineData(Jit.RyuJit, Platform.X86, RyuJitNotAvailable)]
//      [InlineData(Jit.RyuJit, Platform.X64, RyuJitNotAvailable)]
//      public void CheckMono(Jit jit, Platform platform, string errorMessage)
//      {
//          Verify(Runtime.Mono, jit, platform, errorMessage);
//      }

        [Theory]
        [InlineData(Jit.LegacyJit, Platform.X86, ToolchainSupportsOnlyRyuJit)]
        [InlineData(Jit.LegacyJit, Platform.X64, ToolchainSupportsOnlyRyuJit)]
        [InlineData(Jit.RyuJit, Platform.X64, null)]
        public void CheckCore(Jit jit, Platform platform, string errorMessage)
        {
            Verify(CoreRuntime.Core70, jit, platform, errorMessage);
        }

        private void Verify(Runtime runtime, Jit jit, Platform platform, string errorMessage)
        {
            var logger = new OutputLogger(Output);
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry.WithPlatform(platform).WithJit(jit).WithRuntime(runtime))
                .AddLogger(logger)
                .AddColumnProvider(DefaultColumnProviders.Instance);

            CanExecute<TestBenchmark>(config, fullValidation: errorMessage is null);

            if (errorMessage is not null)
            {
                Assert.Contains(errorMessage, logger.GetLog());
            }
        }

        public class TestBenchmark
        {
            [Benchmark]
            public void Benchmark() { }
        }
    }
}