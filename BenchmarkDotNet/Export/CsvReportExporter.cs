using System.Collections.Generic;
using BenchmarkDotNet.Logging;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Export
{
    public class CsvReportExporter : IReportExporter
    {
        public static CsvReportExporter Default = new CsvReportExporter();

        private CsvReportExporter()
        {
        }

        public void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger)
        {
            var table = ReportExporterHelper.BuildTable(reports, false);
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