using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.IntegratedExporter;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class IntegratedExporterConfigBaseAttribute : Attribute, IConfigSource
    {
        // CLS-Compliant Code requires a constructor without an array in the argument list
        [PublicAPI]
        protected IntegratedExporterConfigBaseAttribute()
        {
            Config = ManualConfig.CreateEmpty();
        }

        protected IntegratedExporterConfigBaseAttribute(IntegratedExportType exporter)
        {
            Config = ManualConfig.CreateEmpty().SetIntegratedExporterType(exporter);
        }

        public IConfig Config { get; }
    }
}