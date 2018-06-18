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

        public bool Predicate(BenchmarkCase benchmarkCase)
            => typeNames.Any(methodName =>
            {
                var displayName = benchmarkCase.Descriptor.Type.GetDisplayName();
                var fullName = benchmarkCase.Descriptor.Type.FullName;

                return displayName.ContainsWithIgnoreCase(methodName) || fullName.ContainsWithIgnoreCase(methodName);
            });
    }
}