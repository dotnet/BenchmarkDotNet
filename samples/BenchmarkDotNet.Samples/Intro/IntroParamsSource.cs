using System.Collections.Generic;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples.Intro
{
    public class IntroParamsSource
    {
        [ParamsSource(nameof(ValuesForA))]
        public int A { get; set; } // property with public setter

        [ParamsSource(nameof(ValuesForB))]
        public int B; // public field

        public IEnumerable<int> ValuesForA => new[] { 100, 200 }; // public property

        public static IEnumerable<int> ValuesForB() => new[] { 10, 20 }; // public static method

        [Benchmark]
        public void Benchmark() => Thread.Sleep(A + B + 5);
    }
}