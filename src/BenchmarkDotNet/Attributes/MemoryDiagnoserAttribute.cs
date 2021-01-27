using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MemoryDiagnoserAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public MemoryDiagnoserAttribute()
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(MemoryDiagnoser.Default);
        }
    }
}