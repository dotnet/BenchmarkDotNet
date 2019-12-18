using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Exporters.Csv
{
    public class CsvExporter : ExporterBase
    {
        private readonly SummaryStyle style;
        private readonly CsvSeparator separator;
        protected override string FileExtension => "csv";

        public static readonly IExporter Default = new CsvExporter(CsvSeparator.CurrentCulture, SummaryStyle.Default.WithZeroMetricValuesInContent());

        public CsvExporter(CsvSeparator separator) : this (separator, SummaryStyle.Default.WithZeroMetricValuesInContent())
        {
        }

        [PublicAPI] public CsvExporter(CsvSeparator separator, SummaryStyle style)
        {
            this.style = style;
            this.separator = separator;
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            string realSeparator = separator.ToRealSeparator();
            foreach (var line in summary.GetTable(style.WithCultureInfo(summary.GetCultureInfo())).FullContentWithHeader)
            {
                for (int i = 0; i < line.Length;)
                {
                    logger.Write(CsvHelper.Escape(line[i], realSeparator));

                    if (++i < line.Length)
                    {
                        logger.Write(realSeparator);
                    }
                }

                logger.WriteLine();
            }
        }
    }
}