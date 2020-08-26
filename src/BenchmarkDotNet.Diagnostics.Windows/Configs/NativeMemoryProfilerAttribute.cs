using System;
using BenchmarkDotNet.Configs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnostics.Windows.Configs
{
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class)]
    public class NativeMemoryProfilerAttribute : Attribute, IConfigSource
    {
        public NativeMemoryProfilerAttribute()
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new NativeMemoryProfiler());
        }

        public IConfig Config { get; }
    }
}