using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Filters
{
    /// <summary>
    /// filters benchmarks by provided glob patterns
    /// </summary>
    public class GlobFilter : IFilter
    {
        private readonly Regex[] patterns;

        public GlobFilter(string[] patterns)  => this.patterns = patterns.Select(pattern => new Regex(pattern)).ToArray();

        public bool Predicate(BenchmarkCase benchmarkCase)
            => patterns.Any(pattern =>
            {
                var method = benchmarkCase.Descriptor.WorkloadMethod;
                var fullName = $"{method.DeclaringType.FullName}.{method.Name}";
                var displayName = $"{method.DeclaringType.GetDisplayName()}.{method.Name}"; // full name can return sth like BenchmarkDotNet.Tests.SomeGeneric`1[[System.Int32, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]].Create

                return pattern.IsMatch(fullName) || pattern.IsMatch(displayName);
            });
    }
}