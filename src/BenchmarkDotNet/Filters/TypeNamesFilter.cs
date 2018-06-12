using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Filters
{
    /// <summary>
    /// filters benchmarks by provided type names
    /// </summary>
    public class TypeNamesFilter : IFilter
    {
        private readonly string[] typeNames;

        public TypeNamesFilter(string[] typeNames) => this.typeNames = typeNames;

        public bool Predicate(Benchmark benchmark)
            => typeNames.Any(methodName =>
            {
                var displayName = benchmark.Target.Type.GetDisplayName();
                var fullName = benchmark.Target.Type.FullName;

                return displayName.ContainsWithIgnoreCase(methodName) || fullName.ContainsWithIgnoreCase(methodName);
            });
    }
}