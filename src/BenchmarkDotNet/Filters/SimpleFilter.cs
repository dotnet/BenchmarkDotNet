using System;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Filters
{
    public class SimpleFilter : IFilter
    {
        private readonly Func<BenchmarkCase, bool> predicate;

        [PublicAPI]
        public SimpleFilter(Func<BenchmarkCase, bool> predicate) => this.predicate = predicate;

        [PublicAPI]
        public bool Predicate(BenchmarkCase benchmarkCase) => predicate(benchmarkCase);
    }
}