using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters.Csv
{
    public class CsvExporter : ExporterBase
    {
        private readonly string separator;
        private readonly ISummaryStyle style;
        protected override string FileExtension => "csv";

        public static readonly IExporter Default = new CsvExporter(CsvSeparator.CurrentCulture, SummaryStyle.Default);

        public CsvExporter(CsvSeparator separator, ISummaryStyle style)
        {
            this.separator = separator.ToRealSeparator();
            this.style = style;
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            foreach (var line in summary.GetTable(style).FullContentWithHeader)
            {
                for (int i = 0; i < line.Length;)
                {
                    logger.Write(CsvHelper.Escape(line[i]));

                    if (++i < line.Length)
                    {
                        logger.Write(separator);
                    }
                }

                logger.WriteLine();
            }
        }
    }
}