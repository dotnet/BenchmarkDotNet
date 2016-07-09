using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes.Exporters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class ExporterConfigBaseAttribute : Attribute, IConfigSource
    {
        protected ExporterConfigBaseAttribute(params IExporter[] exporters)
        {
            Config = ManualConfig.CreateEmpty().With(exporters);
        }

        public IConfig Config { get; }
    }
}