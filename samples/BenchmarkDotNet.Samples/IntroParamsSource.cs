using System.Collections.Generic;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class IntroParamsSource
    {
        // property with public setter
        [ParamsSource(nameof(ValuesForA))]
        public int A { get; set; }

        // public field
        [ParamsSource(nameof(ValuesForB))]
        public int B;

        // public property
        public IEnumerable<int> ValuesForA => new[] { 100, 200 };

        // public static method
        public static IEnumerable<int> ValuesForB() => new[] { 10, 20 };

        // public field getting its params from a method in another type
        [ParamsSource(typeof(ParamsValues), nameof(ParamsValues.ValuesForC))]
        public int C;

        [Benchmark]
        public void Benchmark() => Thread.Sleep(A + B + C + 5);
    }

    public static class ParamsValues
    {
        public static IEnumerable<int> ValuesForC() => new[] { 1000, 2000 };
    }
}