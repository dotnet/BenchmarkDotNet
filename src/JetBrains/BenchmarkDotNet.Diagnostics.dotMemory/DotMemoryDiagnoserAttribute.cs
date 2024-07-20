using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Diagnostics.dotMemory
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DotMemoryDiagnoserAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public DotMemoryDiagnoserAttribute()
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new DotMemoryDiagnoser());
        }

        public DotMemoryDiagnoserAttribute(string? nugetUrl = null, string? toolsDownloadFolder = null)
        {
            var nugetUri = nugetUrl == null ? null : new Uri(nugetUrl);
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new DotMemoryDiagnoser(nugetUri, toolsDownloadFolder));
        }
    }
}