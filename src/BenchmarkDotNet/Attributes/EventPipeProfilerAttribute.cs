using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EventPipeProfilerAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public EventPipeProfilerAttribute(EventPipeProfile profile)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new EventPipeProfiler(profile));
        }
    }
}