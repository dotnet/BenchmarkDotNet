using BenchmarkDotNet.Exporters;
using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Attributes
{
    public class RPlotExporterAttribute : ExporterConfigBaseAttribute
    {

        public RPlotExporterAttribute() : base(new RPlotExporter())
        {
        }
    }
}