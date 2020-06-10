using System;
using BenchmarkDotNet.Configs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnostics.Windows.Configs
{
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class)]
    public class ConcurrencyVisualizerProfilerAttribute : Attribute, IConfigSource
    {
        public ConcurrencyVisualizerProfilerAttribute() => Config = ManualConfig.CreateEmpty().AddDiagnoser(new ConcurrencyVisualizerProfiler());

        public IConfig Config { get; }
    }
}