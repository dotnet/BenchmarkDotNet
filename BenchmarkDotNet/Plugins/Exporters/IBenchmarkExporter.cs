using System.Collections.Generic;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Plugins.ResultExtenders;

namespace BenchmarkDotNet.Plugins.Exporters
{
    public interface IBenchmarkExporter : IPlugin
    {
        void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger, IEnumerable<IBenchmarkResultExtender> resultExtenders = null);
        IEnumerable<string> ExportToFile(IList<BenchmarkReport> reports, string competitionName, IEnumerable<IBenchmarkResultExtender> resultExtenders = null);
    }
}