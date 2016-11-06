using System;
using System.Collections.Generic;
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
        private static readonly CharacteristicPresenter Presenter = CharacteristicPresenter.SummaryPresenter;

        protected override string FileExtension => "csv";
        protected override string FileCaption => "measurements";

        public string Separator { get; }

        public static readonly CsvMeasurementsExporter Default = new CsvMeasurementsExporter(CsvSeparator.CurrentCulture);

        public CsvMeasurementsExporter(CsvSeparator separator)
        {
            Separator = separator.ToRealSeparator();
        }

        private static MeasurementColumn[] BuildColumns()
        {
            var columns = new List<MeasurementColumn>();

            // Target
            columns.Add(new MeasurementColumn("Target", (summary, report, m) => report.Benchmark.Target.Type.Name + "." +report.Benchmark.Target.MethodDisplayInfo));
            columns.Add(new MeasurementColumn("Target_Namespace", (summary, report, m) => report.Benchmark.Target.Type.Namespace));
            columns.Add(new MeasurementColumn("Target_Type", (summary, report, m) => report.Benchmark.Target.Type.Name));
            columns.Add(new MeasurementColumn("Target_Method", (summary, report, m) => report.Benchmark.Target.MethodDisplayInfo));

            // Job
            foreach (var characteristic in CharacteristicHelper.GetAllPresentableCharacteristics(typeof(Job), true))
                columns.Add(new MeasurementColumn("Job_" + characteristic.Id, (summary, report, m) => Presenter.ToPresentation(report.Benchmark.Job, characteristic)));
            columns.Add(new MeasurementColumn("Job_Display", (summary, report, m) => report.Benchmark.Job.DisplayInfo));

            // Params
            columns.Add(new MeasurementColumn("Params", (summary, report, m) => report.Benchmark.Parameters.PrintInfo));

            // Measurements
            columns.Add(new MeasurementColumn("Measurement_LaunchIndex", (summary, report, m) => m.LaunchIndex.ToString()));
            columns.Add(new MeasurementColumn("Measurement_IterationMode", (summary, report, m) => m.IterationMode.ToString()));
            columns.Add(new MeasurementColumn("Measurement_IterationIndex", (summary, report, m) => m.IterationIndex.ToString()));
            columns.Add(new MeasurementColumn("Measurement_Nanoseconds", (summary, report, m) => m.Nanoseconds.ToStr()));
            columns.Add(new MeasurementColumn("Measurement_Operations", (summary, report, m) => m.Operations.ToString()));
            columns.Add(new MeasurementColumn("Measurement_Value", (summary, report, m) => (m.Nanoseconds / m.Operations).ToStr()));

            return columns.ToArray();
        }

        private static readonly Lazy<MeasurementColumn[]> lazyColumns = new Lazy<MeasurementColumn[]>(BuildColumns);

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

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            var columns = lazyColumns.Value;
            logger.WriteLine(string.Join(Separator, columns.Select(c => CsvHelper.Escape(c.Title))));

            foreach (var report in summary.Reports)
            {
                foreach (var measurement in report.AllMeasurements)
                {
                    for (int i = 0; i < columns.Length; )
                    {
                        logger.Write(CsvHelper.Escape(columns[i].GetValue(summary, report, measurement)));

                        if (++i < columns.Length)
                        {
                            logger.Write(Separator);
                        }
                    }
                    logger.WriteLine();
                }
            }
        }

        public static Job[] GetJobs(Summary summary) => summary.Benchmarks.Select(b => b.Job).ToArray();
    }
}