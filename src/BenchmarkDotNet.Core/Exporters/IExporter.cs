using System.Collections.Generic;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters
{
    public interface IExporter
    {
        string Name { get; }

        void ExportToLog(Summary summary, ILogger logger);
        IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger);
    }
}