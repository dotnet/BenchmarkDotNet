using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters.Csv
{
    public class CsvExporter : ExporterBase
    {
        private readonly ISummaryStyle style;
        private readonly CsvSeparator separator;
        protected override string FileExtension => "csv";

        public static readonly IExporter Default = new CsvExporter(CsvSeparator.CurrentCulture, SummaryStyle.Default);

        public CsvExporter(CsvSeparator separator) : this (separator, SummaryStyle.Default)
        {
        }

        public CsvExporter(CsvSeparator separator, ISummaryStyle style)
        {
            this.style = style;
            this.separator = separator;
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            string realSeparator = separator.ToRealSeparator();
            foreach (var line in summary.GetTable(style).FullContentWithHeader)
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