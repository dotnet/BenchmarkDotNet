using System;
using BenchmarkDotNet.Configs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnostics.Windows.Configs
{
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class)]
    public class NativeMemoryDiagnoserAttribute : Attribute, IConfigSource
    {
        public NativeMemoryDiagnoserAttribute()
        {
            Config = ManualConfig.CreateEmpty().With(new NativeMemoryDiagnoser());
        }

        public IConfig Config { get; }
    }
}