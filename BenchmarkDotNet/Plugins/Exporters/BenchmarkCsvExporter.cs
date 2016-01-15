using System.Collections.Generic;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.ResultExtenders;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Exporters
{
    public class BenchmarkCsvExporter : IBenchmarkExporter
    {
        public string Name => "csv";
        public string Description => "Csv exporter";

        public static readonly IBenchmarkExporter Default = new BenchmarkCsvExporter();

        private BenchmarkCsvExporter()
        {
        }

        public void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger, IEnumerable<IBenchmarkResultExtender> resultExtenders = null)
        {
            var table = BenchmarkExporterHelper.BuildTable(reports, resultExtenders, false, true);
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

        public void ExportToFile(IList<BenchmarkReport> reports, string competitionName, IEnumerable<IBenchmarkResultExtender> resultExtenders = null)
        {
            BenchmarkExporterHelper.ExportToFile(this, reports, competitionName, resultExtenders);
        }
    }
}