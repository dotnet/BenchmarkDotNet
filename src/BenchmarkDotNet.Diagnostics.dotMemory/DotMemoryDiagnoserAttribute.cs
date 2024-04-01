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

        public DotMemoryDiagnoserAttribute(Uri? nugetUrl = null, string? toolsDownloadFolder = null)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new DotMemoryDiagnoser(nugetUrl, toolsDownloadFolder));
        }
    }
}