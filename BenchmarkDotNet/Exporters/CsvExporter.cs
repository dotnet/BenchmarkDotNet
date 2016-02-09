using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public class CsvExporter : ExporterBase
    {
        protected override string FileExtension => "csv";

        public static readonly IExporter Default = new CsvExporter();

        private CsvExporter()
        {
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            foreach (var line in summary.Table.FullContentWithHeader)
                logger.WriteLine(string.Join(";", line));
        }
    }
}