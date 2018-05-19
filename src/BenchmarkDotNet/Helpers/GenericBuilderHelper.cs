using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Helpers
{
    public static class GenericBuilderHelper
    {
        public static Type[] GetRunnableBenchmarks(IEnumerable<Type> types)
            => types.Where(type => type.ContainsRunnableBenchmarks())
                    .SelectMany(t => t.BuildGenericsIfNeeded())
                    .Where(x => x.isSuccesed)
                    .Select(x => x.result)
                    .ToArray();
    }
}