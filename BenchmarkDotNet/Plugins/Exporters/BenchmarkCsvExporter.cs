using System.Collections.Generic;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Exporters
{
    public class BenchmarkCsvExporter : BenchmarkExporterBase
    {
        public override string Name => "csv";
        public override string Description => "Csv exporter";

        public override string FileExtension => "csv";

        public static readonly IBenchmarkExporter Default = new BenchmarkCsvExporter();

        private BenchmarkCsvExporter()
        {
        }

        public override void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger)
        {
            var table = BenchmarkExporterHelper.BuildTable(reports, false);
            foreach (var line in table)
            {
                for (int i = 0; i < line.Length; i++)
                {
                    if (i != 0)
                        logger.Write(";");
                    logger.Write(line[i]);
                }
                logger.NewLine();
            }
        }
    }
}