using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

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
            yield return new object[] { new int[] { 1, 2, 3 }, 4 };
            yield return new object[] { Enumerable.Range(0, 100).ToArray(), 4 };
            yield return new object[] { Enumerable.Range(0, 100).ToArray(), 101 };
        }
    }
}