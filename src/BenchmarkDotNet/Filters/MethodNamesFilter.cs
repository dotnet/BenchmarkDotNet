using System.Linq;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    /// <summary>
    /// filters benchmarks by provided method names
    /// </summary>
    public class MethodNamesFilter : IFilter
    {
        private readonly string[] methodNames;

        public MethodNamesFilter(string[] methodNames) => this.methodNames = methodNames;

        public bool Predicate(BenchmarkCase benchmarkCase)
            => methodNames.Any(methodName =>
            {
                var method = benchmarkCase.Descriptor.WorkloadMethod;
                var fullName = $"{method.DeclaringType.FullName}.{method.Name}";

                return method.Name.ContainsWithIgnoreCase(methodName) || fullName.ContainsWithIgnoreCase(methodName);
            });
    }
}