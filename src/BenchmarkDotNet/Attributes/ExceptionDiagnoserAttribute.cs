using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExceptionDiagnoserAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public ExceptionDiagnoserAttribute() => Config = ManualConfig.CreateEmpty().AddDiagnoser(ExceptionDiagnoser.Default);
    }
}
