using System;
using System.Linq;
using BenchmarkDotNet.Plugins;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Tasks;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ProcessorArchitectureTest
    {
        const string FailedCaption = "FAILED";
        const string AnyCpuOkCaption = "AnyCpuOkCaption";
        const string HostPlatformOkCaption = "HostPlatformOkCaption";

        [Fact]
        public void SpecifiedProccesorArchitectureMustBeRespected()
        {
            var logger = new BenchmarkAccumulationLogger();
            var plugins = BenchmarkPluginBuilder.CreateDefault().AddLogger(logger).Build();
            var reports = new BenchmarkRunner(plugins).Run<ProcessorArchitectureTest>().ToArray();
            var testLog = logger.GetLog();

            Assert.True(reports.Any());
            Assert.True(reports.All(report => report.Runs.Count > 1));

            Assert.DoesNotContain(FailedCaption, testLog);
            Assert.Contains(AnyCpuOkCaption, testLog);
            Assert.Contains(HostPlatformOkCaption, testLog);
        }

        [Benchmark]
        [BenchmarkTask(platform: BenchmarkPlatform.X86, mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void _32Bit()
        {
            if (IntPtr.Size != 4)
            {
                throw new InvalidOperationException(FailedCaption);
            }
        }

        [Benchmark]
        [BenchmarkTask(platform: BenchmarkPlatform.X64, mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void _64Bit()
        {
            if (IntPtr.Size != 8)
            {
                throw new InvalidOperationException(FailedCaption);
            }
        }

        [Benchmark]
        [BenchmarkTask(platform: BenchmarkPlatform.AnyCpu, mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void AnyCpu()
        {
            Console.WriteLine(AnyCpuOkCaption);
        }

        [Benchmark]
        [BenchmarkTask(platform: BenchmarkPlatform.HostPlatform, mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void Host()
        {
            Console.WriteLine(HostPlatformOkCaption);
        }
    }
}