using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BenchmarkDotNet.Exporters.IntegratedExporter
{
    public class IntegratedExporter : IIntegratedExporter
    {
        private readonly IntegratedExportType integratedExporterTypes;
        public IntegratedExporter(IntegratedExportType integratedExporterTypes)
        {
            this.integratedExporterTypes = integratedExporterTypes;
        }
        public string Name => nameof(IntegratedExporter);

        public IEnumerable<string> ExportToFiles(Func<IEnumerable<string>> callback)
        {
            var result = callback();
            return result;
        }

        public void ExportToLog(Action callback)
        {
            callback();
        }

        public IntegratedExportType GetIntegratedExportType() => integratedExporterTypes;
    }


}
