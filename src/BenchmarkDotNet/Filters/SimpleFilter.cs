using System;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    public class SimpleFilter : IFilter
    {
        private readonly Func<BenchmarkCase, bool> predicate;

        public SimpleFilter(Func<BenchmarkCase, bool> predicate)
        {
            this.predicate = predicate;
        }

        public bool Predicate(BenchmarkCase benchmarkCase) => predicate(benchmarkCase);
    }
}