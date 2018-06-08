using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class IntroArgumentsSource
    {
        [Benchmark]
        [ArgumentsSource(nameof(Numbers))]
        public double Pow(double x, double y) => Math.Pow(x, y);

        public IEnumerable<object[]> Numbers()
        {
            yield return new object[] { 1.0, 1.0 };
            yield return new object[] { 2.0, 2.0 };
            yield return new object[] { 4.0, 4.0 };
            yield return new object[] { 10.0, 10.0 };
        }
    }
}