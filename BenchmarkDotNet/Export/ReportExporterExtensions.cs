using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Logging;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Export
{
    public static class ReportExporterExtensions
    {
        public static void SaveToFile(this IReportExporter reportExporter, List<BenchmarkReport> reports, string fileName)
        {
            using (var logStreamWriter = new StreamWriter(fileName))
            {
                var logger = new BenchmarkStreamLogger(logStreamWriter);
                reportExporter.Export(reports, logger);
            }    
        }
    }
}