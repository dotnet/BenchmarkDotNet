using BenchmarkDotNet.Exporters.IntegratedExporter;
using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Attributes.Exporters
{
    public class IntegratedExporterAttribute : IntegratedExporterConfigBaseAttribute
    {
        protected IntegratedExporterAttribute()
        { }

        public IntegratedExporterAttribute(IntegratedExportType integratedExporterType) : base(integratedExporterType)
        {
        }
    }
}