using System;
using BenchmarkDotNet.Configs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnostics.Windows.Configs
{
    [PublicAPI]
    public class ConcurrencyVisualizerProfilerAttribute : Attribute, IConfigSource
    {
        public ConcurrencyVisualizerProfilerAttribute() => Config = ManualConfig.CreateEmpty().AddDiagnoser(new ConcurrencyVisualizerProfiler());

        public IConfig Config { get; }
    }
}