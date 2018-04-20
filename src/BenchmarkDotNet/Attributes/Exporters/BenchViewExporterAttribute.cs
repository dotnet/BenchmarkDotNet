using System;
using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes.Exporters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class BenchViewExporterAttribute : ExporterConfigBaseAttribute
    {
        public BenchViewExporterAttribute() : base(BenchViewExporter.Default) { }
    }
}