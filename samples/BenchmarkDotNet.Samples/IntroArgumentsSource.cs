using System;
using System.Collections.Generic;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class IntroArgumentsSource
    {
        [Benchmark]
        [ArgumentsSource(nameof(Numbers))]
        public double ManyArguments(double x, double y) => Math.Pow(x, y);

        public IEnumerable<object[]> Numbers() // for multiple arguments it's an IEnumerable of array of objects (object[])
        {
            yield return new object[] { 1.0, 1.0 };
            yield return new object[] { 2.0, 2.0 };
            yield return new object[] { 4.0, 4.0 };
            yield return new object[] { 10.0, 10.0 };
        }

        [Benchmark]
        [ArgumentsSource(typeof(BenchmarkArguments), nameof(BenchmarkArguments.TimeSpans))] // when the arguments come from a different type, specify that type here
        public void SingleArgument(TimeSpan time) => Thread.Sleep(time);
    }

    public static class BenchmarkArguments
    {
        public static IEnumerable<object> TimeSpans() // for single argument it's an IEnumerable of objects (object)
        {
            yield return TimeSpan.FromMilliseconds(10);
            yield return TimeSpan.FromMilliseconds(100);
        }
    }
}