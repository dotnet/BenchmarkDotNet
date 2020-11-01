using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ThreadingDiagnoserAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public ThreadingDiagnoserAttribute() => Config = ManualConfig.CreateEmpty().AddDiagnoser(ThreadingDiagnoser.Default);
    }
}