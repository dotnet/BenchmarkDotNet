using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.ResultExtenders;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Exporters
{
    public class BenchmarkCsvRunsExporter : BenchmarkExporterBase
    {
        public override string Name => "csv";
        public override string Description => "Csv exporter";

        public override string FileExtension => "csv";
        public override string FileCaption => "runs";

        public static readonly IBenchmarkExporter Default = new BenchmarkCsvRunsExporter();

        private BenchmarkCsvRunsExporter()
        {
        }

        private class Column
        {
            public string Title { get; }
            public Func<BenchmarkReport, BenchmarkRunReport, string> GetValue { get; }

            public Column(string title, Func<BenchmarkReport, BenchmarkRunReport, string> getValue)
            {
                Title = title;
                GetValue = getValue;
            }
        }

        private readonly List<Column> columns = new List<Column>
        {
            new Column("BenchmarkType", (report, run) => report.Benchmark.Target.Type.Name),
            new Column("BenchmarkMethod", (report, run) => report.Benchmark.Target.MethodTitle),
            new Column("BenchmarkMode", (report, run) => report.Benchmark.Task.Configuration.Mode.ToString()),
            new Column("BenchmarkPlatform", (report, run) => report.Benchmark.Task.Configuration.Platform.ToString()),
            new Column("BenchmarkJitVersion", (report, run) => report.Benchmark.Task.Configuration.JitVersion.ToString()),
            new Column("BenchmarkFramework", (report, run) => report.Benchmark.Task.Configuration.Framework.ToString()),
            new Column("BenchmarkToolchain", (report, run) => report.Benchmark.Task.Configuration.Toolchain.ToString()),
            new Column("BenchmarkRuntime", (report, run) => report.Benchmark.Task.Configuration.Runtime.ToString()),
            new Column("RunIterationMode", (report, run) => run.IterationMode.ToString()),
            new Column("RunIterationIndex", (report, run) => run.IterationIndex.ToString()),
            new Column("RunNanoseconds", (report, run) => run.Nanoseconds.ToStr()),
            new Column("RunOperations", (report, run) => run.Operations.ToString()),
        };

        public override void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger, IEnumerable<IBenchmarkResultExtender> resultExtenders = null)
        {
            logger.WriteLine(string.Join(";", columns.Select(c => c.Title)));
            foreach (var report in reports)
                foreach (var run in report.Runs)
                    logger.WriteLine(string.Join(";", columns.Select(column => column.GetValue(report, run))));
        }
    }
}