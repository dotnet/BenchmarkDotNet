using System;
using System.Collections.Generic;

namespace BenchmarkDotNet
{
    public class Benchmark : IBenchmark
    {
        public string Name { get; }
        public Action Initialize { get; }
        public Action Action { get; }
        public Action Clean { get; }
        public Dictionary<string, object> Settings { get; }

        public Benchmark(string name, Action initialize, Action action, Action clean, Dictionary<string, object> settings = null)
        {
            Name = name;
            Initialize = initialize;
            Action = action;
            Clean = clean;
            Settings = settings;
        }

        public Benchmark(string name, Action action, Dictionary<string, object> settings = null)
        {
            Name = name;
            Initialize = null;
            Action = action;
            Clean = null;
            Settings = settings;
        }
    }
}