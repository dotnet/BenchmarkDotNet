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

        public bool Predicate(BenchmarkCase benchmarkCase)
        {
            var customTypeAttributes = benchmarkCase.Descriptor.Type.GetCustomAttributes(true).Select(attribute => attribute.GetType()).ToArray();
            var customMethodsAttributes = benchmarkCase.Descriptor.WorkloadMethod.GetCustomAttributes(true).Select(attribute => attribute.GetType()).ToArray();

            var allCustomAttributes = customTypeAttributes.Union(customMethodsAttributes).Distinct().ToArray();

            return attributes.Any(attributeName => allCustomAttributes.Any(attribute => attribute.Name.StartsWith(attributeName, StringComparison.InvariantCultureIgnoreCase)));
        }
    }
}