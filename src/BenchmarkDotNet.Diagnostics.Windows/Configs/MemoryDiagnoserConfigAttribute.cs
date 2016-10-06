using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Diagnostics.Windows.Configs
{
    public class MemoryDiagnoserConfigAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public MemoryDiagnoserConfigAttribute()
        {
            Config = ManualConfig.CreateEmpty().With(new MemoryDiagnoser());
        }
    }
}