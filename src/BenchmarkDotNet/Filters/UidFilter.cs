using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    /// <summary>
    /// filters benchmarks by unique id.
    /// </summary>
    public class UidFilter : IFilter
    {
        private readonly string[] uids;

        public UidFilter(string[] uids)
        {
            this.uids = uids;
        }

        public bool Predicate(BenchmarkCase benchmarkCase)
        {
            var uid = benchmarkCase.GetUniqueId();
            return uids.Any(x => x.Equals(uid, StringComparison.OrdinalIgnoreCase));
        }
    }
}
