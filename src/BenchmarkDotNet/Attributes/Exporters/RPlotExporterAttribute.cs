using BenchmarkDotNet.Exporters;
using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Attributes
{
    public class RPlotExporterAttribute : ExporterConfigBaseAttribute
    {
        protected RPlotExporterAttribute()
        {}

        public RPlotExporterAttribute(params IntegratedExportEnum[] integratedExportEnums) : base(new RPlotExporter(integratedExportEnums))
        {
        }
    }
}