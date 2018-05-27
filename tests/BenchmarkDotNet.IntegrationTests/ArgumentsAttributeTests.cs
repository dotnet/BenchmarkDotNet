using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ArgumentsAttributeTests : BenchmarkTestExecutor
    {
        public ArgumentsAttributeTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ArgumentsArePassedToBenchmarks() => CanExecute<WithArguments>();

        public class WithArguments
        {
            [Benchmark]
            [Arguments(true, 1)]
            [Arguments(false, 2)]
            public void Simple(bool boolean, int number)
            {
                if (boolean && number != 1 || !boolean && number != 2)
                    throw new InvalidOperationException("Incorrect values were passed");
            }
        }

        [Fact]
        public void ArgumentsFromSourceArePassedToBenchmarks() => CanExecute<WithArgumentsSource>();

        public class WithArgumentsSource
        {
            [Benchmark]
            [ArgumentsSource(nameof(ArgumentsProvider))]
            public void Simple(bool boolean, int number)
            {
                if (boolean && number != 1 || !boolean && number != 2)
                    throw new InvalidOperationException("Incorrect values were passed");
            }

            public IEnumerable<object[]> ArgumentsProvider()
            {
                yield return new object[] { true, 1 };
                yield return new object[] { false, 2 };
            }
        }

        [Fact]
        public void ArgumentsCanBePassedByReferenceToBenchmark() => CanExecute<WithRefArguments>();

        public class WithRefArguments
        {
            [Benchmark]
            [Arguments(true, 1)]
            [Arguments(false, 2)]
            public void Simple(ref bool boolean, ref int number)
            {
                if (boolean && number != 1 || !boolean && number != 2)
                    throw new InvalidOperationException("Incorrect values were passed");
            }
        }

        [Fact]
        public void NonCompileTimeConstantsCanBeReturnedFromSource() => CanExecute<WithComplexTypesReturnedFromSources>();

        public class WithComplexTypesReturnedFromSources
        {
            [ParamsSource(nameof(DictionaryAsParam))]
            public Dictionary<int, string> DictionaryParamInstance;

            [ParamsSource(nameof(SameButStatic))]
            public Dictionary<int, string> DictionaryParamStatic;

            [Benchmark]
            [ArgumentsSource(nameof(NonPrimitive))]
            public void Simple(SomeClass someClass, SomeStruct someStruct)
            {
                if (DictionaryParamInstance[1234] != "it's an instance getter")
                    throw new InvalidOperationException("Incorrect dictionary (instance");

                if (DictionaryParamStatic[1234] != "it's a static getter")
                    throw new InvalidOperationException("Incorrect dictionary (static)");

                if (!(someStruct.RangeEnd == 100 || someStruct.RangeEnd == 1000))
                    throw new InvalidOperationException("Incorrect struct values were passed");

                if (someStruct.RangeEnd != someClass.Values.Length)
                    throw new InvalidOperationException("Incorrect length");

                for (int i = 0; i < someStruct.RangeEnd; i++)
                    if (someClass.Values[i] != i * 2)
                        throw new InvalidOperationException("Incorrect array values were passed");
            }

            public IEnumerable<object[]> NonPrimitive()
            {
                yield return new object[] { new SomeClass(Enumerable.Range(0, 100).ToArray()), new SomeStruct(100) };
                yield return new object[] { new SomeClass(Enumerable.Range(0, 1000).ToArray()), new SomeStruct(1000) };
            }

            public IEnumerable<object> DictionaryAsParam => new object[] { new Dictionary<int, string>() { { 1234, "it's an instance getter" } } };

            public static IEnumerable<object> SameButStatic => new object[] { new Dictionary<int, string>() { { 1234, "it's a static getter" } } };

            public class SomeClass
            {
                public SomeClass(int[] initialValues) => Values = initialValues.Select(val => val * 2).ToArray();

                public int[] Values { get; }
            }

            public struct SomeStruct
            {
                public SomeStruct(int rangeEnd) => RangeEnd = rangeEnd;

                public int RangeEnd { get; }
            }
        }

        [Fact]
        public void ArrayCanBeUsedAsArgument() => CanExecute<WithArray>();

        public class WithArray
        {
            [Benchmark]
            [Arguments(new[] { 0, 1, 2 })]
            public void AcceptingArray(int[] array)
            {
                if (array.Length != 3)
                    throw new InvalidOperationException("Incorrect array length");

                for (int i = 0; i < 3; i++)
                    if (array[i] != i)
                        throw new InvalidOperationException($"Incorrect array element at index {i}, was {array[i]} instead of {i}");
            }
        }
    }
}