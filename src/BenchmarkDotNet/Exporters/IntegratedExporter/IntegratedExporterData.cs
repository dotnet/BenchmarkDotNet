using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Exporters.IntegratedExporter
{
    public class IntegratedExporterData
    {
        public IExporter Exporter { get; set; }
        public IExporter WithExporter { get; set; }
        public List<IExporter>? Dependencies { get; set; }
    }
}
