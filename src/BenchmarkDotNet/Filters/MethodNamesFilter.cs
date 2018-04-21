using System.Linq;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    public class MethodNamesFilter : IFilter
    {
        private readonly string[] methodNames;

        public MethodNamesFilter(string[] methodNames) => this.methodNames = methodNames;

        public bool Predicate(Benchmark benchmark)
            => methodNames.Any(methodName =>
            {
                var method = benchmark.Target.Method;
                var fullName = $"{method.DeclaringType.FullName}.{method.Name}";

                return method.Name.ContainsWithIgnoreCase(methodName) || fullName.ContainsWithIgnoreCase(methodName);
            });
    }
}