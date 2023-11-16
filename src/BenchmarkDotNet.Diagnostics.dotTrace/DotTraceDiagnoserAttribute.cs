using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Diagnostics.dotTrace
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DotTraceDiagnoserAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public DotTraceDiagnoserAttribute()
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new DotTraceDiagnoser());
        }

        public DotTraceDiagnoserAttribute(Uri? nugetUrl = null, string? toolsDownloadFolder = null)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new DotTraceDiagnoser(nugetUrl, toolsDownloadFolder));
        }
    }
}