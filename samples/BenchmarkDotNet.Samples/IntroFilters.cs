using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;

namespace BenchmarkDotNet.Samples
{
    [DryJob]
    [Config(typeof(Config))]
    public class IntroFilters
    {
        private class Config : ManualConfig
        {
            // We will benchmark ONLY method with
            // names (which contains "A" OR "1") AND (have length < 3)
            public Config()
            {
                // benchmark with names which contains "A" OR "1"
                AddFilter(new DisjunctionFilter(
                    new NameFilter(name => name.Contains("A")),
                    new NameFilter(name => name.Contains("1"))
                ));

                // benchmark with names with length < 3
                AddFilter(new NameFilter(name => name.Length < 3));
            }
        }

        [Benchmark] public void A1() => Thread.Sleep(10); // Will be benchmarked
        [Benchmark] public void A2() => Thread.Sleep(10); // Will be benchmarked
        [Benchmark] public void A3() => Thread.Sleep(10); // Will be benchmarked
        [Benchmark] public void B1() => Thread.Sleep(10); // Will be benchmarked
        [Benchmark] public void B2() => Thread.Sleep(10);
        [Benchmark] public void B3() => Thread.Sleep(10);
        [Benchmark] public void C1() => Thread.Sleep(10); // Will be benchmarked
        [Benchmark] public void C2() => Thread.Sleep(10);
        [Benchmark] public void C3() => Thread.Sleep(10);
        [Benchmark] public void Aaa() => Thread.Sleep(10);
    }
}