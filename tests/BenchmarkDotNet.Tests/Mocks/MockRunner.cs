using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Mocks.Toolchain;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Mocks
{
    public static class MockRunner
    {
        public static Summary Run<T>(ITestOutputHelper output, IMockMeasurer measurer)
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
            exporter.ExportToLog(summary, logger);
            output.WriteLine(logger.GetLog());

            return summary;
        }
    }
}