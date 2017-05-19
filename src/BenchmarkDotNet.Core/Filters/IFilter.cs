using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    public interface IFilter
    {
        bool Predicate(Benchmark benchmark);
    }
}