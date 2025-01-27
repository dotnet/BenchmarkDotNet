using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Diagnostics.dotMemory;

[AttributeUsage(AttributeTargets.Class)]
public class DotMemoryDiagnoserAttribute : Attribute, IConfigSource
{
    public IConfig Config { get; }

    public DotMemoryDiagnoserAttribute()
    {
        var diagnoser = new DotMemoryDiagnoser();
        Config = ManualConfig.CreateEmpty().AddDiagnoser(diagnoser);
    }

    public DotMemoryDiagnoserAttribute(Uri? nugetUrl, string? downloadTo = null)
    {
        var diagnoser = new DotMemoryDiagnoser(nugetUrl, downloadTo);
        Config = ManualConfig.CreateEmpty().AddDiagnoser(diagnoser);
    }
}