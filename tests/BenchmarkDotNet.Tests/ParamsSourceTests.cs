using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class ParamsSourceTests
    {
        // #1809
        [Fact]
        public void NullIsSupportedAsElementOfParamsSource()
        {
            BenchmarkConverter.TypeToBenchmarks(typeof(ParamsSourceWithNull));
        }

        public class ParamsSourceWithNull
        {
            public static IEnumerable<object> Values()
            {
                yield return null;
                yield return ValueTuple.Create(10);
                yield return ValueTuple.Create(10, 20);
                yield return (10, 20, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            }

            [ParamsSource(nameof(Values))]
            public object O { get; set; }

            [Benchmark]
            public object FooBar() => O;
        }
    }
}