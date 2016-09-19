using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters.Csv
{
    public class CsvExporter : ExporterBase
    {
        private readonly string separator;
        protected override string FileExtension => "csv";

        public static readonly IExporter Default = new CsvExporter(CsvSeparator.CurrentCulture);

        public CsvExporter(CsvSeparator separator)
        {
            this.separator = separator.ToRealSeparator();
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            foreach (var line in summary.Table.FullContentWithHeader)
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