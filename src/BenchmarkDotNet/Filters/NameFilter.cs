using System;

namespace BenchmarkDotNet.Filters
{
    public class NameFilter : SimpleFilter
    {
        public NameFilter(Func<string, bool> predicate) : base(b => predicate(b.Descriptor.WorkloadMethod.Name)) { }
    }
}