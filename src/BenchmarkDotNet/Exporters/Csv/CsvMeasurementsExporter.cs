using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Exporters.Csv
{
    public class CsvMeasurementsExporter : ExporterBase
    {
        public static readonly CsvMeasurementsExporter Default = new CsvMeasurementsExporter(CsvSeparator.CurrentCulture, SummaryStyle.Default);
        [PublicAPI] public static CsvMeasurementsExporter WithStyle(SummaryStyle style) => new CsvMeasurementsExporter(CsvSeparator.CurrentCulture, style);

        private static readonly CharacteristicPresenter Presenter = CharacteristicPresenter.SummaryPresenter;

        private static readonly Lazy<MeasurementColumn[]> Columns = new Lazy<MeasurementColumn[]>(BuildColumns);

        private readonly CsvSeparator separator;
        public CsvMeasurementsExporter(CsvSeparator separator, SummaryStyle? style = null)
        {
            this.separator = separator;
            Style = style ?? SummaryStyle.Default;
        }

        public string Separator => separator.ToRealSeparator();

        protected override string FileExtension => "csv";

        protected override string FileCaption => "measurements";

        [PublicAPI] public SummaryStyle Style { get; }

        [PublicAPI] public static Job[] GetJobs(Summary summary) => summary.BenchmarksCases.Select(b => b.Job).ToArray();

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            string realSeparator = Separator;
            var columns = GetColumns(summary);
            logger.WriteLine(string.Join(realSeparator, columns.Select(c => CsvHelper.Escape(c.Title, realSeparator))));

            foreach (var report in summary.Reports)
            {
                foreach (var measurement in report.AllMeasurements)
                {
                    for (int i = 0; i < columns.Length; )
                    {
                        logger.Write(CsvHelper.Escape(columns[i].GetValue(summary, report, measurement), realSeparator));

                        if (++i < columns.Length)
                        {
                            logger.Write(realSeparator);
                        }
                    }
                    logger.WriteLine();
                }
            }
        }

        private static MeasurementColumn[] GetColumns(Summary summary)
        {
            if (!summary.BenchmarksCases.Any(benchmark => benchmark.Config.HasMemoryDiagnoser()))
                return Columns.Value;

            var columns = new List<MeasurementColumn>(Columns.Value)
            {
                new MeasurementColumn("Gen_0", (_, report, __) => report.GcStats.Gen0Collections.ToString(summary.GetCultureInfo())),
                new MeasurementColumn("Gen_1", (_, report, __) => report.GcStats.Gen1Collections.ToString(summary.GetCultureInfo())),
                new MeasurementColumn("Gen_2", (_, report, __) => report.GcStats.Gen2Collections.ToString(summary.GetCultureInfo())),
                new MeasurementColumn("Allocated_Bytes", (_, report, __) => report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase)?.ToString(summary.GetCultureInfo()) ?? MetricColumn.UnknownRepresentation)
            };

            return columns.ToArray();
        }

        private static MeasurementColumn[] BuildColumns()
        {
            // Target
            var columns = new List<MeasurementColumn>
            {
                new MeasurementColumn("Target", (summary, report, m) => report.BenchmarkCase.Descriptor.Type.Name + "." + report.BenchmarkCase.Descriptor.WorkloadMethodDisplayInfo),
                new MeasurementColumn("Target_Namespace", (summary, report, m) => report.BenchmarkCase.Descriptor.Type.Namespace),
                new MeasurementColumn("Target_Type", (summary, report, m) => report.BenchmarkCase.Descriptor.Type.Name),
                new MeasurementColumn("Target_Method", (summary, report, m) => report.BenchmarkCase.Descriptor.WorkloadMethodDisplayInfo)
            };

            // Job
            foreach (var characteristic in CharacteristicHelper.GetAllPresentableCharacteristics(typeof(Job), true))
                columns.Add(new MeasurementColumn("Job_" + characteristic.Id, (summary, report, m) => Presenter.ToPresentation(report.BenchmarkCase.Job, characteristic)));
            columns.Add(new MeasurementColumn("Job_Display", (summary, report, m) => report.BenchmarkCase.Job.DisplayInfo));

            // Params
            columns.Add(new MeasurementColumn("Params", (summary, report, m) => report.BenchmarkCase.Parameters.PrintInfo));

            // Measurements
            columns.Add(new MeasurementColumn("Measurement_LaunchIndex", (summary, report, m) => m.LaunchIndex.ToString()));
            columns.Add(new MeasurementColumn("Measurement_IterationMode", (summary, report, m) => m.IterationMode.ToString()));
            columns.Add(new MeasurementColumn("Measurement_IterationStage", (summary, report, m) => m.IterationStage.ToString()));
            columns.Add(new MeasurementColumn("Measurement_IterationIndex", (summary, report, m) => m.IterationIndex.ToString()));
            columns.Add(new MeasurementColumn("Measurement_Nanoseconds", (summary, report, m) => m.Nanoseconds.ToString("0.##", summary.GetCultureInfo())));
            columns.Add(new MeasurementColumn("Measurement_Operations", (summary, report, m) => m.Operations.ToString()));
            columns.Add(new MeasurementColumn("Measurement_Value", (summary, report, m) => (m.Nanoseconds / m.Operations).ToString("0.##", summary.GetCultureInfo())));

            return columns.ToArray();
        }

        private struct MeasurementColumn
        {
            public string Title { get; }
            public Func<Summary, BenchmarkReport, Measurement, string> GetValue { get; }

            public MeasurementColumn(string title, Func<Summary, BenchmarkReport, Measurement, string> getValue)
            {
                Title = title;
                GetValue = getValue;
            }
        }
    }
}