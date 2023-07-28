using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Mocks.Toolchain;
using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Tests.Builders;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Mocks
{
    public static class MockRunner
    {
        public static Summary Run<T>(ITestOutputHelper output, Func<string, double[]> measurer, IConfig? config = null)
        {
            return Run<T>(output, benchmarkCase =>
                measurer(benchmarkCase.Descriptor.WorkloadMethod.Name)
                    .Select((value, i) => new Measurement(1, IterationMode.Workload, IterationStage.Result, i, 1, value))
                    .ToList(), config);
        }

        public static Summary Run<T>(ITestOutputHelper output, Func<BenchmarkCase, List<Measurement>> measurer, IConfig? config = null)
        {
            var job = new Job("MockJob")
            {
                Infrastructure =
                {
                    Toolchain = new MockToolchain(measurer)
                }
            }.Freeze();

            var logger = new AccumulationLogger();

            var targetConfig = DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .AddJob(job)
                .AddLogger(logger);
            if (config != null)
                targetConfig.Add(config);

            Summary summary;
            using (new HostEnvironmentInfoBuilder().OverrideStaticInstanceCookie())
            {
                summary = BenchmarkRunner.Run<T>(targetConfig);
            }

            var exporter = MarkdownExporter.Mock;
            exporter.ExportToLog(summary, logger);
            output.WriteLine(logger.GetLog());

            return summary;
        }
    }
}