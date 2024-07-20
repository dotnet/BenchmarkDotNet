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

        public DotTraceDiagnoserAttribute(string? nugetUrl = null, string? toolsDownloadFolder = null)
        {
            var nugetUri = nugetUrl == null ? null : new Uri(nugetUrl);
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new DotTraceDiagnoser(nugetUri, toolsDownloadFolder));
        }
    }
}