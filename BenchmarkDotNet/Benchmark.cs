using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet
{
    public class Benchmark : IComparable<Benchmark>
    {
        public BenchmarkTarget Target { get; }
        public BenchmarkTask Task { get; }

        public string Caption => Target.Caption + "_" + Task.Caption;
        public string Description => $"{Target.Description} ({Task.Description})";
        public override string ToString() => Description;
        public IEnumerable<BenchmarkProperty> Properties => Target.Properties.Union(Task.Properties);

        public Benchmark(BenchmarkTarget target, BenchmarkTask task)
        {
            Target = target;
            Task = task;
        }

        public int CompareTo(Benchmark other)
        {
            return string.Compare(Task.Configuration.Caption + Target.Caption, other.Task.Configuration.Caption + other.Target.Caption, StringComparison.Ordinal);
        }
    }
}