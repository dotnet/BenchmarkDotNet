using System;
using System.Linq;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    /// <summary>
    /// filters benchmarks by provided attribute names
    /// </summary>
    public class AttributesFilter : IFilter
    {
        private readonly string[] attributes;

        public AttributesFilter(string[] attributes) => this.attributes = attributes;

        public bool Predicate(Benchmark benchmark)
        {
            var customTypeAttributes = benchmark.Target.Type.GetCustomAttributes(true).Select(attribute => attribute.GetType()).ToArray();
            var customMethodsAttributes = benchmark.Target.Method.GetCustomAttributes(true).Select(attribute => attribute.GetType()).ToArray();

            var allCustomAttributes = customTypeAttributes.Union(customMethodsAttributes).Distinct().ToArray();

            return attributes.Any(attributeName => allCustomAttributes.Any(attribute => attribute.Name.StartsWith(attributeName, StringComparison.InvariantCultureIgnoreCase)));
        }
    }
}