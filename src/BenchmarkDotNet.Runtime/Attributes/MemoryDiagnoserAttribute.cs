using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Attributes
{
    public class MemoryDiagnoserAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public MemoryDiagnoserAttribute()
        {
            Config = ManualConfig.CreateEmpty().With(MemoryDiagnoser.Default);
        }
    }
}