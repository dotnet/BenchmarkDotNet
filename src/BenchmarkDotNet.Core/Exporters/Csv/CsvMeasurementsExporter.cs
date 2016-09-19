using System;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters.Csv
{
    public class CsvMeasurementsExporter : ExporterBase
    {
        private readonly string separator;
        private static readonly CharacteristicPresenter Presenter = CharacteristicPresenter.SummaryPresenter;

        protected override string FileExtension => "csv";
        protected override string FileCaption => "measurements";

        public static readonly IExporter Default = new CsvMeasurementsExporter(CsvSeparator.CurrentCulture);

        public CsvMeasurementsExporter(CsvSeparator separator)
        {
            this.separator = separator.ToRealSeparator();
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

        // TODO: rewrite
        private readonly MeasurementColumn[] columns =
        {
            new MeasurementColumn("Target", (summary, report, m) => report.Benchmark.Target.Type.Name + "." +report.Benchmark.Target.MethodDisplayInfo),
            new MeasurementColumn("TargetType", (summary, report, m) => report.Benchmark.Target.Type.Name),
            new MeasurementColumn("TargetMethod", (summary, report, m) => report.Benchmark.Target.MethodDisplayInfo),

            new MeasurementColumn("Job", (summary, report, m) => report.Benchmark.Job.DisplayInfo),
            new MeasurementColumn("JobRunStrategy", (summary, report, m) => Presenter.ToPresentation(report.Benchmark.Job.Run.RunStrategy)),
            new MeasurementColumn("JobPlatform", (summary, report, m) => Presenter.ToPresentation(report.Benchmark.Job.Env.Platform)),
            new MeasurementColumn("JobJit", (summary, report, m) => Presenter.ToPresentation(report.Benchmark.Job.Env.Jit)),
            new MeasurementColumn("JobToolchain", (summary, report, m) => Presenter.ToPresentation(report.Benchmark.Job.Infrastructure.Toolchain)),
            new MeasurementColumn("JobRuntime", (summary, report, m) => Presenter.ToPresentation(report.Benchmark.Job.Env.Runtime)),

            new MeasurementColumn("Params", (summary, report, m) => report.Benchmark.Parameters.PrintInfo),

            new MeasurementColumn("MeasurementLaunchIndex", (summary, report, m) => m.LaunchIndex.ToString()),
            new MeasurementColumn("MeasurementIterationMode", (summary, report, m) => m.IterationMode.ToString()),
            new MeasurementColumn("MeasurementIterationIndex", (summary, report, m) => m.IterationIndex.ToString()),
            new MeasurementColumn("MeasurementNanoseconds", (summary, report, m) => m.Nanoseconds.ToStr()),
            new MeasurementColumn("MeasurementOperations", (summary, report, m) => m.Operations.ToString()),
            new MeasurementColumn("MeasurementValue", (summary, report, m) => (m.Nanoseconds / m.Operations).ToStr())
        };

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            logger.WriteLine(string.Join(separator, columns.Select(c => CsvHelper.Escape(c.Title))));

            foreach (var report in summary.Reports)
            {
                foreach (var measurement in report.AllMeasurements)
                {
                    for (int i = 0; i < columns.Length; )
                    {
                        logger.Write(CsvHelper.Escape(columns[i].GetValue(summary, report, measurement)));

                        if (++i < columns.Length)
                        {
                            logger.Write(separator);
                        }
                    }
                    logger.WriteLine();
                }
            }
        }

        public static Job[] GetJobs(Summary summary) => summary.Benchmarks.Select(b => b.Job).ToArray();
    }
}