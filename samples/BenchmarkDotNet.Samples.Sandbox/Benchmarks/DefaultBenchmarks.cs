using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Samples.Sandbox;

public class DefaultBenchmarks
{
    private readonly Consumer Consumer = new();

    // [Params(1_000_000)]
    public int N { get; set; } = 1_000_000;

    private readonly IEnumerable<int> EnumerableDataSource;
    private readonly int[] ArrayDataSource;

    public DefaultBenchmarks()
    {
        var rand = new Random(0);
        var source = Enumerable.Range(1, N).Select(x => rand.Next());

        EnumerableDataSource = source;
        ArrayDataSource = EnumerableDataSource.ToArray();
    }

    [Benchmark(Baseline = true)]
    public void Benchmark01()
    {
        foreach (var i in EnumerableDataSource.OrderBy(x => x))
        {
            Consumer.Consume(i);
        }
    }

    [Benchmark]
    public void Benchmark02()
    {
        foreach (var i in ArrayDataSource.OrderBy(x => x))
        {
            Consumer.Consume(i);
        }
    }
}
