using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
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

        public static IEnumerable<object[]> Arguments()
        {
            Platform current = RuntimeInformation.GetCurrentPlatform();

            if (RuntimeInformation.IsFullFramework && current is Platform.X64 or Platform.X86)
            {
                // RoslynToolchain (used for Full Framework) supports building and running for different architecture than the host process
                yield return new object[]
                {
                    current is Platform.X64 ? Platform.X86 : Platform.X64,
                    current is Platform.X64 ? typeof(Benchmark_32bit) : typeof(Benchmark_64bit)
                };
            }

            yield return new object[] { current, IntPtr.Size == 8 ? typeof(Benchmark_64bit) : typeof(Benchmark_32bit) };
            yield return new object[] { Platform.AnyCpu, typeof(AnyCpuBenchmark) };
        }

        [Theory]
        [MemberData(nameof(Arguments))]
        public void SpecifiedProcessorArchitectureMustBeRespected(Platform platform, Type benchmark)
        {
            var config = ManualConfig.CreateEmpty()
                    .AddJob(Job.Dry.WithPlatform(platform))
                    .AddLogger(new OutputLogger(Output)); // make sure we get an output in the TestRunner log

            // CanExecute ensures that at least one benchmark has executed successfully
            CanExecute(benchmark, config, fullValidation: true);
        }

        public class Benchmark_32bit
        {
            [Benchmark]
            public void Verify()
            {
                if (IntPtr.Size != 4)
                {
                    throw new InvalidOperationException("32 bit failed!");
                }
            }
        }

        public class Benchmark_64bit
        {
            [Benchmark]
            public void Verify()
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