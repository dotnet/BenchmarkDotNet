using System;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    public class SimpleFilter : IFilter
    {
        private readonly Func<Benchmark, bool> predicate;

        public SimpleFilter(Func<Benchmark, bool> predicate)
        {
            this.predicate = predicate;
        }

        public bool Predicate(Benchmark benchmark) => predicate(benchmark);
    }
}