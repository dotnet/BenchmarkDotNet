using System;
using BenchmarkDotNet.Configs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnostics.Windows.Configs
{
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class)]
    public class JitStatsDiagnoserAttribute : Attribute, IConfigSource
    {
        public JitStatsDiagnoserAttribute()
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new JitStatsDiagnoser());
        }

        public IConfig Config { get; }
    }
}