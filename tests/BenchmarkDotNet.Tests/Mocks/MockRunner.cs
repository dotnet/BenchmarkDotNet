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
using System.Threading;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Mocks
{
    public static class MockRunner
    {
        public static Summary Run<T>(ITestOutputHelper output, Func<string, double[]> measurer)
            => Run<T>(output, benchmarkCase => measurer(benchmarkCase.Descriptor.WorkloadMethod.Name)
                        .Select((value, i) => new Measurement(1, IterationMode.Workload, IterationStage.Result, i, 1, value))
                        .ToList());

        public static Summary Run<T>(ITestOutputHelper output, Func<BenchmarkCase, List<Measurement>> measurer)
        {
            var job = new Job("MockJob")
            {
                Infrastructure =
                {
                    Toolchain = new MockToolchain(measurer)
                }
            }.Freeze();

            var logger = new AccumulationLogger();

            var config = DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .AddJob(job)
                .AddLogger(logger);
            var summary = BenchmarkRunner.Run<T>(config);

            var exporter = MarkdownExporter.Mock;
            ((ExporterBase)exporter).ExportToLogAsync(summary, logger, CancellationToken.None).AsTask().GetAwaiter().GetResult();
            output.WriteLine(logger.GetLog());

            return summary;
        }
    }
}