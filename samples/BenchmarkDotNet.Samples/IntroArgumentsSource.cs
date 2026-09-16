using System.Runtime.CompilerServices;
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

        [Benchmark]
        [ArgumentsSource(nameof(NumbersAsync))]
        public double AsyncSourcedArguments(double x, double y) => Math.Pow(x, y);

        // the source may be an IAsyncEnumerable, which BenchmarkDotNet awaits, so the values can be produced
        // asynchronously without resorting to blocking sync-over-async in the source. It may take an optional
        // [EnumeratorCancellation] CancellationToken, which receives the benchmark's cancellation token while
        // the values are enumerated.
        public static async IAsyncEnumerable<object[]> NumbersAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            yield return new object[] { 1.0, 1.0 };
            yield return new object[] { 2.0, 2.0 };
        }
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