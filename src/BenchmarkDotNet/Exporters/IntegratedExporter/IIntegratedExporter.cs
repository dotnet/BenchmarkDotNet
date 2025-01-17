using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Exporters.IntegratedExporter
{
    public interface IIntegratedExporter
    {
        string Name { get; }
        void ExportToLog(Action callback);
        IEnumerable<string> ExportToFiles(Func<IEnumerable<string>> callback);
        IntegratedExportType GetIntegratedExportType();
    }
}
