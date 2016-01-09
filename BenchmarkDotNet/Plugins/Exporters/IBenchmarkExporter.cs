using System.Collections.Generic;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Exporters
{
    public interface IBenchmarkExporter : IPlugin
    {
        void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger);
        IEnumerable<string> ExportToFile(IList<BenchmarkReport> reports, string fileNamePrefix);
    }
}