using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using Microsoft.Diagnostics.NETCore.Client;

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