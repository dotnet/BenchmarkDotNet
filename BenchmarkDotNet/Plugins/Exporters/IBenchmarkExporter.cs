using System.Collections.Generic;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Exporters
{
    public interface IBenchmarkExporter
    {
        void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger);
    }
}