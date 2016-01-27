using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public class CsvRunsExporter : ExporterBase
    {
        protected override string FileExtension => "csv";
        protected override string FileCaption => "runs";

        public static readonly IExporter Default = new CsvRunsExporter();

        private CsvRunsExporter()
        {
        }

        private class RunColumn
        {
            public string Title { get; }
            public Func<BenchmarkReport, Measurement, string> GetValue { get; }

            public RunColumn(string title, Func<BenchmarkReport, Measurement, string> getValue)
            {
                Title = title;
                GetValue = getValue;
            }
        }

        private readonly List<RunColumn> runColumns = new List<RunColumn>
        {
            new RunColumn("BenchmarkType", (report, run) => report.Benchmark.Target.Type.Name),
            new RunColumn("BenchmarkMethod", (report, run) => report.Benchmark.Target.MethodTitle),
            new RunColumn("BenchmarkMode", (report, run) => report.Benchmark.Job.Mode.ToString()),
            new RunColumn("BenchmarkPlatform", (report, run) => report.Benchmark.Job.Platform.ToString()),
            new RunColumn("BenchmarkJitVersion", (report, run) => report.Benchmark.Job.Jit.ToString()),
            new RunColumn("BenchmarkFramework", (report, run) => report.Benchmark.Job.Framework.ToString()),
            new RunColumn("BenchmarkToolchain", (report, run) => report.Benchmark.Job.Toolchain.ToString()),
            new RunColumn("BenchmarkRuntime", (report, run) => report.Benchmark.Job.Runtime.ToString()),
            new RunColumn("RunIterationMode", (report, run) => run.IterationMode.ToString()),
            new RunColumn("RunIterationIndex", (report, run) => run.IterationIndex.ToString()),
            new RunColumn("RunNanoseconds", (report, run) => run.Nanoseconds.ToStr()),
            new RunColumn("RunOperations", (report, run) => run.Operations.ToString()),
        };

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            logger.WriteLine(string.Join(";", runColumns.Select(c => c.Title)));
            foreach (var report in summary.Reports.Values)
                foreach (var run in report.AllRuns)
                    logger.WriteLine(string.Join(";", runColumns.Select(column => column.GetValue(report, run))));
        }
    }
}