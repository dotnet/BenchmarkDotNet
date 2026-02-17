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
            public static IEnumerable<object?> Values()
            {
                yield return null;
                yield return ValueTuple.Create(10);
                yield return (10, 20);
                yield return (10, 20, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            }

            [ParamsSource(nameof(Values))]
            public required object? O { get; set; }

            [Benchmark]
            public object? FooBar() => O;
        }

        // #2980
        [Fact]
        public void WriteOnlyPropertyDoesThrowNullReferenceException()
        {
            var exception = Assert.Throws<InvalidBenchmarkDeclarationException>(
                () => BenchmarkConverter.TypeToBenchmarks(typeof(ClassWithWriteOnlyProperty)));

            Assert.Contains(nameof(ClassWithWriteOnlyProperty.WriteOnlyValues), exception.Message);
            Assert.Contains("no public, accessible method/property", exception.Message);
        }

        public class ClassWithWriteOnlyProperty
        {
            private int _writeOnlyValue;

            public int WriteOnlyValues
            {
                set { _writeOnlyValue = value; }
            }

#pragma warning disable BDN1305 // Test intentionally uses write-only property
            [ParamsSource(nameof(WriteOnlyValues))]
            public int MyParam { get; set; }
#pragma warning restore BDN1305

            [Benchmark]
            public void Run() { }
        }
    }
}