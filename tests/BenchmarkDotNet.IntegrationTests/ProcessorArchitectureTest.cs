﻿using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ProcessorArchitectureTest : BenchmarkTestExecutor
    {
        const string X86FailedCaption = "// x86FAILED";
        const string X64FailedCaption = "// x64FAILED";
        const string AnyCpuOkCaption = "// AnyCpuOkCaption";
        const string HostPlatformOkCaption = "// HostPlatformOkCaption";
        const string BenchmarkNotFound = "// There are no benchmarks found";

        public ProcessorArchitectureTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void SpecifiedProccesorArchitectureMustBeRespected()
        {
#if !CORE // dotnet cli does not support x86 compilation so far, so I disable this test
            Verify(Platform.X86, typeof(X86Benchmark), X86FailedCaption);
#endif
            Verify(Platform.X64, typeof(X64Benchmark), X64FailedCaption);
            Verify(Platform.AnyCpu, typeof(AnyCpuBenchmark), "nvm");
        }

        private void Verify(Platform platform, Type benchmark, string failureText)
        {
            var logger = new OutputLogger(Output);

            var config = ManualConfig.CreateEmpty()
                    .With(Job.Dry.With(platform))
                    .With(logger); // make sure we get an output in the TestRunner log

            CanExecute(benchmark, config);

            var testLog = logger.GetLog();
            Assert.DoesNotContain(failureText, testLog);
            Assert.DoesNotContain(BenchmarkNotFound, testLog);
        }

        public class X86Benchmark
        {
            [Benchmark]
            public void _32Bit()
            {
                if (IntPtr.Size != 4)
                {
                    throw new InvalidOperationException(X86FailedCaption);
                }
            }
        }

        public class X64Benchmark
        {
            [Benchmark]
            public void _64Bit()
            {
                if (IntPtr.Size != 8)
                {
                    throw new InvalidOperationException(X64FailedCaption);
                }
            }
        }

        public class AnyCpuBenchmark
        {
            [Benchmark]
            public void AnyCpu()
            {
                Console.WriteLine(AnyCpuOkCaption);
            }
        }

        public class HostBenchmark
        {
            [Benchmark]
            public void Host()
            {
                Console.WriteLine(HostPlatformOkCaption);
            }
        }
    }
}