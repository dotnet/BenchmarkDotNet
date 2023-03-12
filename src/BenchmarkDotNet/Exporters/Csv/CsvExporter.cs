using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Exporters.Csv
{
    public class CsvExporter : ExporterBase
    {
        private readonly CsvSeparator separator;
        private readonly SummaryStyle? style;
        protected override string FileExtension => "csv";

        public static readonly IExporter Default = new CsvExporter(CsvSeparator.CurrentCulture);

        [PublicAPI]
        public CsvExporter(CsvSeparator separator, SummaryStyle? style = null)
        {
            this.separator = separator;
            this.style = style;
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            string realSeparator = separator.ToRealSeparator();
            var exportStyle = (style ?? summary.Style).WithZeroMetricValuesInContent();
            foreach (var line in summary.GetTable(exportStyle).FullContentWithHeader)
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