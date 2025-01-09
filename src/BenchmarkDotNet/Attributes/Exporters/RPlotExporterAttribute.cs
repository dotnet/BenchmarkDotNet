using BenchmarkDotNet.Exporters;
using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Attributes
{
    public class RPlotExporterAttribute : ExporterConfigBaseAttribute
    {
        /// <param name="reportDependencies">
        /// Determines whether Rplot exported images should be added to the HTML report (a.k.a HTML report).
        /// False by default (rplotImages will not be displayed).
        /// </param>
        protected RPlotExporterAttribute()
        {}

        public RPlotExporterAttribute(params IntegratedExportEnum[] integratedExportEnums) : base(new RPlotExporter(integratedExportEnums))
        {
        }
    }
}