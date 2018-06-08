using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Code;

namespace BenchmarkDotNet.Samples
{
    public class IntroArrayParam
    {
        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public int ArrayIndexOf(int[] array, int value)
            => Array.IndexOf(array, value);

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public int ManualIndexOf(int[] array, int value)
        {
            for (int i = 0; i < array.Length; i++)
                if (array[i] == value)
                    return i;

            return -1;
        }

        public IEnumerable<object[]> Data()
        {
            yield return CreateArrayAndValue(new[] { 1, 2, 3 }, 4);
            yield return CreateArrayAndValue(Enumerable.Range(0, 100).ToArray(), 4);
            yield return CreateArrayAndValue(Enumerable.Range(0, 100).ToArray(), 101);
        }

        private object[] CreateArrayAndValue(int[] array, int value)
            => new object[] { ArrayParam<int>.ForPrimitives(array), value };
    }
}