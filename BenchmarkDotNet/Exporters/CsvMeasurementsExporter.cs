using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public class CsvMeasurementsExporter : ExporterBase
    {
        protected override string FileExtension => "csv";
        protected override string FileCaption => "measurements";

        public static readonly IExporter Default = new CsvMeasurementsExporter();

        private CsvMeasurementsExporter()
        {
        }

        private class MeasurementColumn
        {
            public string Title { get; }
            public Func<Summary, BenchmarkReport, Measurement, string> GetValue { get; }

            public MeasurementColumn(string title, Func<Summary, BenchmarkReport, Measurement, string> getValue)
            {
                Title = title;
                GetValue = getValue;
            }
        }

        // TODO: add params
        private readonly List<MeasurementColumn> columns = new List<MeasurementColumn>
        {
            new MeasurementColumn("Target", (summary, report, m) => report.Benchmark.Target.Type.Name + "." +report.Benchmark.Target.MethodTitle),
            new MeasurementColumn("TargetType", (summary, report, m) => report.Benchmark.Target.Type.Name),
            new MeasurementColumn("TargetMethod", (summary, report, m) => report.Benchmark.Target.MethodTitle),

            new MeasurementColumn("Job", (summary, report, m) => report.Benchmark.Job.GetShortInfo(GetJobs(summary))),
            new MeasurementColumn("JobMode", (summary, report, m) => report.Benchmark.Job.Mode.ToString()),
            new MeasurementColumn("JobPlatform", (summary, report, m) => report.Benchmark.Job.Platform.ToString()),
            new MeasurementColumn("JobJit", (summary, report, m) => report.Benchmark.Job.Jit.ToString()),
            new MeasurementColumn("JobFramework", (summary, report, m) => report.Benchmark.Job.Framework.ToString()),
            new MeasurementColumn("JobToolchain", (summary, report, m) => report.Benchmark.Job.Toolchain.ToString()),
            new MeasurementColumn("JobRuntime", (summary, report, m) => report.Benchmark.Job.Runtime.ToString()),

            new MeasurementColumn("Params", (summary, report, m) => report.Benchmark.Parameters.PrintInfo),

            new MeasurementColumn("MeasurementLaunchIndex", (summary, report, m) => m.LaunchIndex.ToString()),
            new MeasurementColumn("MeasurementIterationMode", (summary, report, m) => m.IterationMode.ToString()),
            new MeasurementColumn("MeasurementIterationIndex", (summary, report, m) => m.IterationIndex.ToString()),
            new MeasurementColumn("MeasurementNanoseconds", (summary, report, m) => m.Nanoseconds.ToStr()),
            new MeasurementColumn("MeasurementOperations", (summary, report, m) => m.Operations.ToString()),
            new MeasurementColumn("MeasurementValue", (summary, report, m) => (m.Nanoseconds / m.Operations).ToStr()),
        };

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            logger.WriteLine(string.Join(";", columns.Select(c => c.Title)));
            foreach (var report in summary.Reports.Values)
                foreach (var measurement in report.AllMeasurements)
                    logger.WriteLine(string.Join(";", columns.Select(column => column.GetValue(summary, report, measurement))));
        }


        public static IJob[] GetJobs(Summary summary) => summary.Benchmarks.Select(b => b.Job).ToArray();
    }
}