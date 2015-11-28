using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Exporters
{
    public static class BenchmarkExporterExtensions
    {
        public static void SaveToFile(this IBenchmarkExporter benchmarkExporter, List<BenchmarkReport> reports, string fileName)
        {
            using (var logStreamWriter = new StreamWriter(fileName))
            {
                var logger = new BenchmarkStreamLogger(logStreamWriter);
                benchmarkExporter.Export(reports, logger);
            }    
        }
    }
}