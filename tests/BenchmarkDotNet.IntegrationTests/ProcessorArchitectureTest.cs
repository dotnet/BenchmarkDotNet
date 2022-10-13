using System;
using BenchmarkDotNet.Attributes;
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
        public ProcessorArchitectureTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void SpecifiedProcessorArchitectureMustBeRespected()
        {
#if NETFRAMEWORK // dotnet cli does not support x86 compilation so far, so I disable this test
            Verify(Platform.X86, typeof(X86Benchmark));
#endif
            Verify(Platform.X64, typeof(X64Benchmark));
            Verify(Platform.AnyCpu, typeof(AnyCpuBenchmark));
        }

        private void Verify(Platform platform, Type benchmark)
        {
            var config = ManualConfig.CreateEmpty()
                    .AddJob(Job.Dry.WithPlatform(platform))
                    .AddLogger(new OutputLogger(Output)); // make sure we get an output in the TestRunner log

            // CanExecute ensures that at least one benchmark has executed successfully
            CanExecute(benchmark, config, fullValidation: true);
        }

        public class X86Benchmark
        {
            [Benchmark]
            public void _32Bit()
            {
                if (IntPtr.Size != 4)
                {
                    throw new InvalidOperationException("32 bit failed!");
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
                    throw new InvalidOperationException("64 bit failed!");
                }
            }
        }

        public class AnyCpuBenchmark
        {
            [Benchmark]
            public void AnyCpu()
            {
            }
        }
    }
}