using System.Collections.Generic;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Exporters
{
    public class BenchmarkCsvExporter : IBenchmarkExporter
    {
        public static readonly BenchmarkCsvExporter Default = new BenchmarkCsvExporter();

        private BenchmarkCsvExporter()
        {
        }

        public void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger)
        {
            var table = BenchmarkExporterHelper.BuildTable(reports, false, true);
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